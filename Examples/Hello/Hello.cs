using Actors;

namespace Examples.Hello;

/// <summary>
/// The classic example, where the "Hello World" string is streamed character-by-character to a
/// <c>Printer</c> actor.
/// 
/// The example demonstrates the basic <c>Actor</c> API and the order-preserving semantics of
/// an actor's <c>Inbox</c>
/// </summary>
public class Example
{
    class Printer : Actor<char>
    {
        protected override Task Perform(Inbox inbox)
        {
            var ch = inbox.Receive();
            Console.Write(ch);
            return Task.CompletedTask;
        }
    }

    public static async Task RunAsync()
    {
        Console.WriteLine("---\nRunning hello example");
        var printer = new Printer();
        foreach (var ch in "Hello World\n")
        {
            printer.Send(ch);
        }
        await printer.Drain();
    }
}