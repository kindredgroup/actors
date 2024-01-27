using System.Collections.Specialized;

class Program
{
    static async Task Main(string[] args)
    {
        var examples = new Dictionary<string, Func<Task>>()
        {
            { "Echo", () => Examples.Echo.Example.RunAsync() },
            { "Throttle", () => Examples.Throttle.Example.RunAsync() },
            { "Counter", () => Examples.Counter.Example.RunAsync() }
        };

        if (args.Length == 1)           // run a specific example
        {
            var exampleName = args[0];
            var example = examples[exampleName];
            await example();
        }
        else if (args.Length == 0)      // run all examples
        {
            foreach (var example in examples.Values)
            {
                await example();
            }
        }
        else
        {
            throw new ArgumentException(String.Format("Invalid arguments: [{0}].", String.Join(", ", args)));
        }
    }
}

