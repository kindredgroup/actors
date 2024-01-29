using System;
using System.Threading.Tasks;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;

namespace Actors.Benchmarks;
#nullable enable

public class EchoBenchmark
{
    class SendPings(int pings)
    {
        public int Pings { get; } = pings;

        public TaskCompletionSource Completion { get; } = new TaskCompletionSource();
    }

    class Ping {}

    class Pong {}

    class PingActor : Actor
    {
        internal PongActor? Opponent { get; set; }

        private int remainingMessages;

        private SendPings? sendPings;

        protected override Task Perform(Inbox inbox)
        {
            switch (inbox.Receive())
            {
                case SendPings message:
                    this.sendPings = message;
                    this.remainingMessages = sendPings.Pings;
                    Opponent!.Send(new Ping());
                    break;

                case Pong message:
                    remainingMessages--;
                    if (remainingMessages > 0)
                    {
                        Opponent!.Send(new Ping());
                    }
                    else
                    {
                        sendPings!.Completion.SetResult();
                    }
                    break;
                
                default:
                    throw new NotSupportedException();
            }
            return Task.CompletedTask;
        }
    }

    class PongActor : Actor
    {
        internal PingActor? Opponent { get; set; }

        protected override Task Perform(Inbox inbox)
        {
            switch (inbox.Receive())
            {
                case Ping message:
                    Opponent!.Send(new Pong());
                    break;
                
                default:
                    throw new NotSupportedException();
            }
            return Task.CompletedTask;
        }
    }

    private readonly PingActor pingActor = new();
    private readonly PongActor pongActor = new();

    [GlobalSetup]
    public void Setup()
    {
        pingActor.Opponent = pongActor;
        pongActor.Opponent = pingActor;
    }

    [Params(1_000)]
    public int numMessages;

    [Benchmark]
    public async Task StartAndWait()
    {
        var sendPings = new SendPings(numMessages);
        pingActor.Send(sendPings);
        await sendPings.Completion.Task;
    }
}
