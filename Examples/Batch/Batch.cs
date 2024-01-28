using System.Runtime.InteropServices;
using Actors;

namespace Examples.Batch;

/// <summary>
/// While actors typically process messages one at a time using <c>Inbox.Receive()</c>,
/// it is also possible to process all pending messages as a batch with 
/// <c>Inbox.ReceiveAll()</c>.
/// 
/// Batch processing has the advantage of look-ahead, wherein an actor can quickly
/// identify messages that may have been superseded. When messages tend to cluster
/// around a small number of entities and where message processing is expensive (e.g.,
/// when a database update is needed), look-ahead lets the actor coalesce work for a
/// more efficient state update.
/// 
/// The example demonstrates batch processing with <c>Inbox.ReceiveAll()</c> and
/// coalescing of updates for efficiency.
/// </summary>
public class Example
{
    /// <summary>
    /// An update instruction targeting a specific financial instrument.
    /// </summary>
    /// <param name="symbol">The stock symbol</param>
    /// <param name="price">The new price.</param>
    class Update(string symbol, decimal price)
    {
        internal string Symbol { get; } = symbol;
        internal decimal Price { get; } = price;
    }

    /// <summary>
    /// A market actor keeps a map of stock symbols to their current price. State
    /// updates are assumed to be expensive; it helps when if can eliminate duplicates.
    /// </summary>
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
                // in practice, this might be a slow database update
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Coalesce a batch by removing duplicate updates.
        /// </summary>
        /// <param name="batch">The batch of messages.</param>
        /// <returns></returns>
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