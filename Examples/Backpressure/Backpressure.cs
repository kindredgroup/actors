using Actors;

namespace Examples.Backpresure;

/// <summary>
/// A scenario where some unbounded source is submitting work to a sink, which takes time to process items. We want to prevent
/// the source from submitting work beyond the sink's processing ability.
/// 
/// The example demonstrates the use of a <c>TaskCompletionSource</c> to communicate completion out of an actor.
/// </summary>
public class Example
{
    /// <summary>
    /// Instruction to perform some background work.
    /// </summary>
    /// <param name="id">Unique identifier of the work item.</param>
    class WorkItem(int id)
    {
        internal TaskCompletionSource Completion { get; } = new();

        internal int Id { get; } = id;
    }

    class SinkActor : Actor<WorkItem>
    {
        protected override async Task Perform(Inbox inbox)
        {
            var workItem = inbox.Receive();
            Console.WriteLine("processing work item {0}", workItem.Id);
            await Task.Delay(1);
            workItem.Completion.SetResult();
        }
    }

    public static async Task RunAsync()
    {
        Console.WriteLine("---\nRunning backpressure example");
        
        const int Messages = 100;
        const int MaxPending = 10;

        var sinkActor = new SinkActor();
        var pendingWork = new Dictionary<int, Task>();
        for (int i = 0; i < Messages; i++)
        {
            var workItem = new WorkItem(i);
            await FreeCapacityAsync(pendingWork, MaxPending);
            pendingWork[workItem.Id] = workItem.Completion.Task;
            Console.WriteLine("submitting work item {0}", workItem.Id);
            sinkActor.Send(workItem);
        }
        await sinkActor.Drain();
    }

    public static async Task FreeCapacityAsync(Dictionary<int, Task> pendingWork, int maxPending)
    {
        if (pendingWork.Count == maxPending)
        {
            Console.WriteLine("waiting for backlog to subside");
            await Task.WhenAny(pendingWork.Values);
            foreach (var entry in pendingWork)
            {
                if (entry.Value.IsCompleted)
                {
                    pendingWork.Remove(entry.Key);
                }
            }
            Console.WriteLine("reduced to {0}", pendingWork.Count);
        }
    }
}