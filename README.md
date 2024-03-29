`actors`
===
[![Build Status](https://img.shields.io/github/actions/workflow/status/kindredgroup/actors/dotnet.yml?branch=master&style=flat-square&logo=github)](https://github.com/kindredgroup/actors/actions/workflows/dotnet.yml)

**Lightweight actor pattern for .NET.**

Actors are units of serial execution within a broader concurrent topology. They enable the safe construction of massively parallel applications without the traditional synchronization primitives (mutexes, semaphores, and so forth). These aren't needed because rather than communicating by sharing state, actors communicate by message passing.

# Hello World
An `Actor` implementation has a dedicated `Inbox` to which messages can be posted in a fire-and-forget fashion via its `Send()` method. They are subsequently delivered to the actor's `Perform()` method in the order of their submission. An actor consumes a message by calling `Receive()` on the inbox.

Below is a classic example, where the "Hello World" string is streamed character-by-character to a simple `Printer` actor. It demonstrates the basic `Actor` API and the order-preserving semantics of an actor's `Inbox`.

```csharp
class Printer : Actor<char>
{
    protected override Task Perform(Inbox inbox)
    {
        var ch = inbox.Receive();
        Console.Write(ch);
        return Task.CompletedTask;
    }
}

// calling the actor
var printer = new Printer();
foreach (var ch in "Hello World\n")
{
    printer.Send(ch);
}
await printer.Drain();
```

`Actor` classes can be optionally typed with the message that the actor expects to receive, as in the above example. If the actor only expects one kind of message, extending the generic `Actor<M>` is recommended for added type safety. More complex actors that handle a variety of message types should extend the non-generic `Actor` variant and cast messages internally. 

# More Examples
See the `Examples` project for a variety of typical actor use cases. Run all examples with

```sh
just run
```

Pass the name of an example to run just that. For example, to run the `Batch` example:

```sh
just run Batch
```

# Performance
Most of the overhead in an actor system is in the scheduling of dormant actors when they receive their first message and the subsequent unscheduling when they process their last. We'll call this the 'slow path'.

When an actor is constantly posted work at a rate close to its throughput, it will remain scheduled and the overhead is one monitor entry per `Send()` and one per `Receive()`. We'll call this the 'fast path'.

Benchmarks were run using the following setup:

|Parameter    |Value
|-------------|-------------------------------------
|Harness      |BenchmarkDotNet v0.13.12             
|OS           |Ubuntu 22.04.3 LTS (Jammy Jellyfish) 
|Framework    |.NET SDK 8.0.101
|Processor    |AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores, virtualised

Results:

|Path             |Time-cost     |Calculation
|-----------------|--------------|---------------
|Fast             |106 ns/op     |`FireAndForgetBenchmark`, time / number of messages
|Slow             |1.3 µs/op     |`EchoBenchmark`, time / number of messages / 2
