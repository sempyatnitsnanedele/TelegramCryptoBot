using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parser;

namespace Parser;

public class Exchanger
{

    static public Dictionary<int, (string Name, double Price, double Change)> assets = new Dictionary<int, (string Name, double price, double Change)>
        {
            { 1, ("Bitcoin", 0, 0)},
            { 1027, ("Ethereum", 0, 0)},
            { 1839, ("BNB", 0, 0)},
            { 2010, ("Cardano", 0, 0)}
        };

    Parse parser = new Parse(assets.Keys.ToArray());
    private static readonly Dictionary<long, CancellationTokenSource> userAlerts = new();
    public CancellationToken StartWork(long chatId)
    {
        CancelWork(chatId);

        var cts = new CancellationTokenSource();
        userAlerts[chatId] = cts;
        return cts.Token;
    }
    public void CancelWork(long chatId)
    {
        if (userAlerts.TryGetValue(chatId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
            userAlerts.Remove(chatId);
        }

    }
    public async Task Exchange()
    {
        var source = new CancellationTokenSource();
        CancellationToken token = source.Token;
        
            parser.OnPriceUpdate += (id, newPrice) =>
            {
                    if (assets.ContainsKey(id))
                    {
                        var oldPrice = assets[id].Price;
                        assets[id] = (assets[id].Name, newPrice, newPrice - oldPrice);

                        
                    }
            };
        while (!token.IsCancellationRequested)
        {
            await parser.RunAsync(token);
        }
    }

}





