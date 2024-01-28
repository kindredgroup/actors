using Actors;

namespace Examples.Echo;

/// <summary>
/// A pair of actors sending ping-pong messages to each other.
/// 
/// The <c>PingActor</c> is instructed externally to begin sending
/// a quantity of pings. One ping is send initially, then another on each
/// receipt of a pong. On the <c>PongActor</c>, a pong is sent on receipt
/// of a ping.
/// 
/// The <c>PingActor</c> responds to a <c>SendPings</c> instruction when
/// it receives the last pong. The external caller awaits the completion
/// of <c>SendPings</c>.
/// 
/// The example demonstrates communication between peer actors, basic actor
/// state (a counter) and responding to a message using an unbounded task.
/// </summary>
public class Example
{
    /// <summary>
    /// An instruction to send a quantity of pings.
    /// </summary>
    /// <param name="pings">The number of pings to send.</param>
    class SendPings(int pings)
    {
        public int Pings { get; } = pings;

        public TaskCompletionSource Completion { get; } = new TaskCompletionSource();
    }

    /// <summary>
    /// A ping message.
    /// </summary>
    class Ping {}

    /// <summary>
    /// A pong message.
    /// </summary>
    class Pong {}

    /// <summary>
    /// Actor responsible for pinging its opponent.
    /// </summary>
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
                    Console.WriteLine($"Will send {message.Pings} pings");
                    this.sendPings = message;
                    this.remainingMessages = sendPings.Pings;
                    Opponent!.Send(new Ping());
                    break;

                case Pong message:
                    Console.WriteLine("<- pong");
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

    /// <summary>
    /// Actor responsible for ponging its opponent.
    /// </summary>
    class PongActor : Actor
    {
        internal PingActor? Opponent { get; set; }

        protected override Task Perform(Inbox inbox)
        {
            switch (inbox.Receive())
            {
                case Ping message:
                    Console.WriteLine("ping ->");
                    Opponent!.Send(new Pong());
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
        pingActor.Opponent = pongActor;
        pongActor.Opponent = pingActor;

        var sendPings = new SendPings(10);
        pingActor.Send(sendPings);
        await sendPings.Completion.Task;
    }
}

