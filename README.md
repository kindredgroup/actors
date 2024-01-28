`actors`
===
[![Build Status](https://img.shields.io/github/actions/workflow/status/kindredgroup/actors/dotnet.yml?branch=master&style=flat-square&logo=github)](https://github.com/kindredgroup/actors/actions/workflows/dotnet.yml)

**Lightweight actor pattern for .NET.**

Actors are units of serial execution within a broader concurrent topology. They enable the safe construction of massively parallel applications without the traditional synchronization primitives (mutexes, semaphores, and so forth). These aren't needed because rather than communicating by sharing state, actors communicate by message passing.

An `Actor` implementation has a dedicated `Inbox` to which messages can be posted in a fire-and-forget fashion via its `Send()` method. They are subsequently delivered to the actor's `Perform()` method in the order of their submission.

# Examples
See the `Examples` project for a variety of typical actor use cases. Run all examples with

```sh
just run
```

Pass the name of an example to run just that. For example, to run the `Batch` example:

```sh
just run Batch
```
