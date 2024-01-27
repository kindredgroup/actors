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

        protected override Task Perform(ActorContext context)
        {
            var batch = context.ReceiveAll();
            Console.WriteLine("processing batch of {0}", batch.Count);
            Dictionary<string, decimal> merge = [];
            foreach (var update in batch)
            {
                ref decimal value = ref CollectionsMarshal.GetValueRefOrAddDefault(merge, update.Symbol, out bool exists);
                if (exists)
                {
                    Console.WriteLine("overwriting {0}", update.Symbol);
                }
                else
                {
                    Console.WriteLine("setting {0}", update.Symbol);
                }
                value = update.Price;
            }
            
            foreach (var entry in merge)
            {
                prices[entry.Key] = entry.Value;
            }
            return Task.CompletedTask;
        }
    }

    public static async Task RunAsync()
    {
        Console.WriteLine("---\nRunning batch");
        var marketActor = new MarketActor();
        string[] symbols = ["AAPL", "MSFT", "GOOGL", "AMZN", "NVDA", "META"];
        var rand = new Random();
        for (int i = 0; i < 200; i++)
        {
            var symbol = symbols[rand.Next() % symbols.Length];
            var price = new Decimal(Math.Abs(rand.NextDouble()));
            marketActor.Send(new Update(symbol, price));
        }
        await marketActor.Drain();
    }
}