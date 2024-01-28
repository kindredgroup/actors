using System.Runtime.InteropServices;
using Actors;

namespace Examples.Batch;

public class Example
{
    class Update(string symbol, decimal price)
    {
        internal string Symbol { get; } = symbol;
        internal decimal Price { get; } = price;
    }

    class MarketActor : Actor<Update>
    {
        private readonly Dictionary<string, decimal> prices = [];

        protected override Task Perform(Inbox inbox)
        {
            var batch = inbox.ReceiveAll();
            var coalesced = Coalesce(batch);
            Console.WriteLine("batch of {0} coalesced to {1}", batch.Count, coalesced.Count);
            foreach (var entry in coalesced)
            {
                prices[entry.Key] = entry.Value;
            }
            return Task.CompletedTask;
        }

        private static Dictionary<string, decimal> Coalesce(List<Update> batch)
        {
            Dictionary<string, decimal> merge = [];
            foreach (var update in batch)
            {
                ref decimal value = ref CollectionsMarshal.GetValueRefOrAddDefault(merge, update.Symbol, out bool exists);
                Console.WriteLine("{0} {1}", exists ? "overwriting" : "setting", update.Symbol);
                value = update.Price;
            }
            return merge;
        }
    }

    public static async Task RunAsync()
    {
        Console.WriteLine("---\nRunning batch");
        var marketActor = new MarketActor();
        string[] symbols = ["AAPL", "MSFT", "GOOGL", "AMZN", "NVDA", "META"];
        var rand = new Random();
        for (int i = 0; i < 100; i++)
        {
            var symbol = symbols[rand.Next() % symbols.Length];
            var price = new Decimal(Math.Abs(rand.NextDouble()));
            marketActor.Send(new Update(symbol, price));

            if (rand.Next() % 10 == 0)  // occasionally pause for the updates to sink in
            {
                await Task.Delay(1);
            }
        }
        await marketActor.Drain();
    }
}