using System;
using System.Threading.Tasks;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;

namespace Actors.Benchmarks;

public class AckDelivery
{
    class WorkItem
    {
        public TaskCompletionSource Completion { get; } = new();
    }

    class AckDeliveryWorker : Actor<WorkItem>
    {
        protected override Task Perform(Inbox inbox)
        {
            var workItem = inbox.Receive();
            workItem.Completion.SetResult();
            return Task.CompletedTask;
        }
    }

    private readonly AckDeliveryWorker worker = new();

    [Benchmark]
    public async Task SendAndAwaitAck()
    {
        var workItem = new WorkItem();
        worker.Send(workItem);
        await workItem.Completion.Task;
    }
}
