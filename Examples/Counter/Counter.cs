using Actors;

namespace Examples.Counter;

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

        protected override Task Perform(ActorContext context)
        {
            switch (context.Receive())
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