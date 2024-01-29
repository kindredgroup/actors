using System;
using System.Threading.Tasks;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;

namespace Actors.Benchmarks;

public class FireAndForgetBenchmark
{
    class FireAndForgetWorker : Actor<int>
    {
        protected override Task Perform(Inbox inbox)
        {
            inbox.Receive();
            return Task.CompletedTask;
        }
    }

    private readonly FireAndForgetWorker worker = new();

    [Params(1_000)]
    public int numMessages;

    [Benchmark]
    public async Task SendAndDrain()
    {
        for (var i = 0; i < numMessages; i++)
        {
            worker.Send(i);
        }
        await worker.Drain();
    }
}
