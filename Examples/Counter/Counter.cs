using Actors;

namespace Examples.Counter;

/// <summary>
/// A simple counter that accepts instructions for mutating its value (by way of
/// <c>Increment</c> and <c>Set</c> messages) and for retrieving its state (using
/// a <c>Get</c> message).
/// 
/// The example demonstrates state manipulation using message passing and state
/// querying using a quasi request-response mechanism. The response part is 
/// implemented with unbounded tasks (using a <c>TaskCompletionSource</c>).
/// </summary>
public class Example
{
    class Increment {}

    class Set(int value)
    {
        internal int Value { get; } = value;
    }

    class Get
    {
        internal TaskCompletionSource<int> Completion { get; } = new TaskCompletionSource<int>();
    }

    class Counter : Actor
    {
        private int count;

        protected override Task Perform(Inbox inbox)
        {
            switch (inbox.Receive())
            {
                case Increment increment:
                    count++;
                    break;

                case Set set:
                    count = set.Value;
                    break;

                case Get get:
                    get.Completion.SetResult(count);
                    break;
                    
                default:
                    throw new NotSupportedException();
            }
            return Task.CompletedTask;
        }
    }

    public static async Task RunAsync()
    {
        Console.WriteLine("---\nRunning counter");
        var counter = new Counter();
        counter.Send(new Set(10));
        for (int i = 0; i < 10; i++)
        {
            counter.Send(new Increment());
        }
        var get = new Get();
        counter.Send(get);
        var count = await get.Completion.Task;
        Console.WriteLine("final count: {0}", count);
    }
}