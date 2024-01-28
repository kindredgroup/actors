namespace Actors.Tests;

public class Barrier
{
    public TaskCompletionSource Entered { get; } = new();
    public TaskCompletionSource Resume { get; } = new();
}

public class PausingActor : ErrorTrappingActor<Barrier>
{       
    protected override async Task Perform(Inbox inbox)
    {
        var barrier = inbox.Receive();
        barrier.Entered.SetResult();
        await barrier.Resume.Task;
    }
}