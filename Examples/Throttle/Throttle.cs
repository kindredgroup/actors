using Actors;

namespace Examples.Throttle;

/// <summary>
/// A topology in which a parent actor delegates partially ordered work items 
/// to child actors to be processed concurrently. (The ordering is determined by
/// taking the work item ID modulo a constant <c>Radix</c>.)
/// 
/// There is a caveat: the parent cannot recruit more than a constant
/// <c>MaxChildren</c> number of child actors to process the work items concurrently.
/// When this number of reached, the parent must hold off from processing further
/// messages until at least one of its children is done with their work, at which point
/// the child can be disposed and a new one spawned in its place.
/// 
/// The example demonstrates a type of supervision strategy where an actor awaits 
/// the 'draining' of its children. While draining is in progress, the supervising actor
/// does not process any messages. This has the effect of throttling the parent, limiting
/// the concurrency factor.
/// 
/// The example also demonstrates a cascading drain, wherein the external caller drains the 
/// parent actor while the latter drains all of its children. The external caller's awaited
/// task completes only when the entire hierarchy has been drained; i.e., when the actor
/// topology has no work remaining.
/// </summary>
public class Example
{
    /// <summary>
    /// Instruction to perform some background work.
    /// </summary>
    /// <param name="id">Unique identifier of the work item.</param>
    class WorkItem(int id)
    {
        internal int Id { get; } = id;
    }

    /// <summary>
    /// Instruction to the parent actor to await the drainage of all of its children.
    /// </summary>
    class DrainAll {}

    /// <summary>
    /// The parent actor, containing an array of child actors. Some slots in
    /// the <c>children</c> array will be <c>null</c>, as there are fewer 
    /// active children than the number of slots in the array.
    /// </summary>
    class Parent : Actor
    {
        /// <summary>
        /// How many ways to divide the work.
        /// </summary>
        private const int Radix = 10;

        /// <summary>
        /// The maximum number of works that may coexist.
        /// </summary>
        private const int MaxChildren = 8;

        /// <summary>
        /// The worker children. (Child labour.)
        /// </summary>
        private readonly Child?[] children = new Child[Radix];

        /// <summary>
        /// The number of currently active child actors.
        /// </summary>
        private int numChildren;

        protected override async Task Perform(Inbox inbox)
        {
            switch (inbox.Receive())
            {
                case WorkItem workItem:
                    await DelegateWork(workItem);
                    break;

                case DrainAll:
                    await DrainAllActorsAsync();
                    break;
                
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Delegates work on the parent actor by identifying
        /// the desired child actor and forwarding the work
        /// item ID to the child.
        /// 
        /// The child may not exist. If there is spare capacity
        /// available, a child is created. Otherwise, the parent
        /// awaits the disposal of one or more of its children.
        /// </summary>
        /// <param name="workItem"></param>
        /// <returns></returns>
        private async Task DelegateWork(WorkItem workItem)
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

        /// <summary>
        /// Awaits the draining of child actors and removes them
        /// from the <c>children</c> array, leaving a <c>null</c> in their place.
        /// 
        /// Draining is considered complete when the actor exhausts its inbox.
        /// </summary>
        /// <returns></returns>
        private async Task DisposeSomeActorsAsync()
        {
            await Troupe<int>.OfNullable(children).DrainAny();

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

        /// <summary>
        /// Drains all child actors, awaiting on all.
        /// </summary>
        /// <returns></returns>
        private async Task DrainAllActorsAsync()
        {
            Console.WriteLine("draining all child actors");
            await Troupe<int>.OfNullable(children).DrainAll();
            Console.WriteLine("drained");
        }
    }

    /// <summary>
    /// A child actor that processes the work handed to it.
    /// </summary>
    /// <param name="id"></param>
    class Child(int id) : Actor<int>
    {
        public int Id { get; } = id;

        protected override Task Perform(Inbox inbox)
        {
            var message = inbox.Receive();
            Console.WriteLine("child {0} working on item {1}", Id, message);
            return Task.Delay(10);
        }
    }
    
    public static async Task RunAsync()
    {
        Console.WriteLine("---\nRunning throttle");
        var parentActor = new Parent();
        const int Messages = 20;
        var rand = new Random();

        for (int i = 0; i < Messages; i++)
        {
            parentActor.Send(new WorkItem(rand.Next()));
        }
        parentActor.Send(new DrainAll());
        await parentActor.Drain();
    }
}