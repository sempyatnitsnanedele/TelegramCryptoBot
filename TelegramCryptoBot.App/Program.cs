using Parser;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Polling;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .Build();

string? botToken = config["BotSettings:Token"];

if (string.IsNullOrEmpty(botToken))
{
    throw new Exception("Токен не найден");
}

Exchanger Check = new Exchanger();


using var cts = new CancellationTokenSource();
var bot = new TelegramBotClient((botToken), cancellationToken: cts.Token);
var me = await bot.GetMe();
bot.OnError += OnError;
bot.OnMessage += OnMessage;
bot.OnUpdate += OnUpdate;
Console.WriteLine($"@{me.Username} is running... Press enter to end");
Check.Exchange();
Console.ReadLine();
cts.Cancel();

var tasks = new List<Task>() { };
async Task OnError(Exception ex, HandleErrorSource src)
{
    Console.WriteLine(ex);
}

async Task OnMessage (Message msg, UpdateType type)
{
    if (msg.Text == "/start")
    {
        var firstmessage = await bot.SendMessage(msg.Chat, "Добро пожаловать в Crypto bot для сайта coinmarketcap.com!",
            replyMarkup: new[] { "Список валют", "Включить уведомления" });


    }
    if (msg.Text == "Список валют")
    {
        var checkmessage = await bot.SendMessage(msg.Chat, "Курс какой валюты вы хотите просмотреть?",
            replyMarkup: new string[][] { ["Bitcoin", "Ethereum"],
                                            ["BNB", "Cardano"],
                                                ["Вернуться"]});
    }
    foreach (var val in Exchanger.assets) {
    if(msg.Text == val.Value.Name)
        {
            await bot.SendMessage(msg.Chat, $"Курс {val.Value.Name}: ${Math.Round(val.Value.Price, 4)}");
        }
        if (msg.Text == $"{val.Value.Name}+")
        {
            await bot.SendMessage(msg.Chat, "Ставим уведомление...");
            await SetAlert(val.Value.Name, msg);
            await bot.SendMessage(msg.Chat, "Успешно!");
        }
    }
    if (msg.Text == "Вернуться")
    {
        var firstmessage = await bot.SendMessage(msg.Chat, "Добро пожаловать в Crypto bot для сайта coinmarketcap.com!",
            replyMarkup: new[] { "Список валют", "Включить уведомления" });
    }
    if (msg.Text == "Включить уведомления")
    {
        var alertmessage = await bot.SendMessage(msg.Chat, "На какую валюту хотите поставить уведомление? (Если цена упадёт или поднимется на 1%, пришлём вам сообщение)",
            replyMarkup: new string[][] { ["Bitcoin+", "Ethereum+"],
                                            ["BNB+","Cardano+"],
                                            ["Снять все уведомления"],
                                                ["Вернуться"]});
    }
    if (msg.Text == "Снять все уведомления")
    {
        Check.CancelWork(msg.Chat.Id);
        await bot.SendMessage(msg.Chat, "Все уведомления сняты!");
    }

}

async Task SetAlert(string coin, Message msg)
{
    long chatId = msg.Chat.Id;
    CancellationToken token = Check.StartWork(chatId);
    int id = 0;
    double firstprice = 0;
    foreach (var item in Exchanger.assets)
    {
        if (item.Value.Name == coin)
        {
            id = item.Key; break;
        }
    }
    firstprice = Exchanger.assets[id].Price;
    double difference = (firstprice * 0.0001);
        Task.Run(async () =>
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (Math.Abs(Exchanger.assets[id].Price - firstprice) > difference)
                    {
                        Check.CancelWork(chatId);
                        await bot.SendMessage(msg.Chat, $"Изменился курс {coin}! Цена: ${Math.Round(Exchanger.assets[id].Price, 4)}");
                    }

                    await Task.Delay(2500, token);
                }
            }
            catch(OperationCanceledException) {}
            catch (Exception ex) { Console.WriteLine($"Ошибка: {ex.Message}"); }
        }, token);
    
}
async Task OnUpdate(Update upd)
{
    if( upd is { CallbackQuery: { } query })
    {
        await bot.AnswerCallbackQuery(query.Id, $"You choosed {query.Data}");
        await bot.SendMessage(query.Message!.Chat, $"User {query.From} clicked on {query.Data}");
    }
}

