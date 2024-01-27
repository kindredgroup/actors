using System.Diagnostics;
using Actors;

namespace Examples.Echo;

public class Example
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
        internal Actor? Friend { get; set; }

        private int remainingMessages;

        private SendPings? sendPings;

        protected override Task Perform(ActorContext context)
        {
            switch (context.Receive())
            {
                case SendPings message:
                    Console.WriteLine($"Will send {message.Pings} pings");
                    this.sendPings = message;
                    this.remainingMessages = sendPings.Pings;
                    Friend!.Send(new Ping());
                    break;

                case Pong message:
                    Console.WriteLine("<- pong");
                    remainingMessages--;
                    if (remainingMessages > 0)
                    {
                        Friend!.Send(new Ping());
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
        internal Actor? Friend { get; set; }

        protected override Task Perform(ActorContext context)
        {
            switch (context.Receive())
            {
                case Ping message:
                    Console.WriteLine("ping ->");
                    Friend!.Send(new Pong());
                    break;
                
                default:
                    throw new NotSupportedException();
            }
            return Task.CompletedTask;
        }
    }

    public static async Task RunAsync()
    {
        Console.WriteLine("---\nRunning echo");
        var pingActor = new PingActor();
        var pongActor = new PongActor();
        pingActor.Friend = pongActor;
        pongActor.Friend = pingActor;

        var start = new SendPings(10);
        pingActor.Send(start);
        await start.Completion.Task;
    }
}

