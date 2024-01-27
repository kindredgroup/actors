using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using Actors;

namespace Examples.Throttle;

public class Example
{
    /// <summary>
    /// Instruction to perform some background work.
    /// </summary>
    /// <param name="id">Unique identifier of the work item.</param>
    class WorkItem(int id)
    {
        public int Id { get; } = id;
    }

    /// <summary>
    /// Instruction to the parent actor to await the drainage of all of its children.
    /// </summary>
    class DrainAll {}

    class Parent : Actor
    {
        private const int Radix = 10;
        private const int MaxChildren = 8;

        private readonly Child?[] children = new Child[Radix];

        private int numChildren;

        protected override async Task Perform(ActorContext context)
        {
            switch (context.Receive())
            {
                case WorkItem workItem:
                    await DoWork(workItem);
                    break;

                case DrainAll:
                    await DrainAllActorsAsync();
                    break;
                
                default:
                    throw new NotSupportedException();
            }
        }

        private async Task DoWork(WorkItem workItem)
        {
            var workId = workItem.Id;
            var childId = workId % Radix;
            Console.WriteLine("parent received item {0}", workId);
            if (children[childId] is null)
            {
                if (numChildren == MaxChildren)
                {
                    Console.WriteLine("throttling");
                    await DisposeSomeActorsAsync();
                }

                Console.WriteLine("spawning child {0}", childId);
                children[childId] = new Child(childId);
                numChildren++;
            }
            children[childId]!.Send(workId);
        }

        private Task[] DrainTasks()
        {
            return children.Where(child => child is not null).Select(child => child!.Drain()).ToArray();
        }

        private async Task DisposeSomeActorsAsync()
        {
            var drainTasks = DrainTasks();
            await Task.WhenAny(drainTasks);

            foreach (var child in children)
            {
                if (!child?.Scheduled ?? false)
                {
                    Console.WriteLine("disposing child {0}", child!.Id);
                    children[child.Id] = null;
                    numChildren--;
                }
            }
        }

        private async Task DrainAllActorsAsync()
        {
            Console.WriteLine("draining all child actors");
            await Task.WhenAll(DrainTasks());
        }

        protected override void OnError(Exception e)
        {
            Console.WriteLine("Error in parent actor: {0}", e);
        }
    }

    class Child(int id) : Actor<int>
    {
        public int Id { get; } = id;

        protected override Task Perform(ActorContext context)
        {
            var message = context.Receive();
            Console.WriteLine("child {0} working on item {1}", Id, message);
            return Task.Delay(10);
        }

        protected override void OnError(Exception e)
        {
            Console.WriteLine("Error in child actor: {0}", e);
        }
    }
    
    public static async Task RunAsync()
    {
        Console.WriteLine("---\nRunning throttle");
        var parentActor = new Parent();
        const int Messages = 100;
        var rand = new Random();

        for (int i = 0; i < Messages; i++)
        {
            parentActor.Send(new WorkItem(rand.Next()));
        }
        parentActor.Send(new DrainAll());
        await parentActor.Drain();
    }
}