namespace Actors.Tests;

[TestClass]
public class ActorTest
{
    class PausingActor : Actor
    {
        internal TaskCompletionSource entered = new();
        internal TaskCompletionSource resume = new();

        internal void Reset()
        {
            entered = new();
            resume = new();
        }
        
        protected override async Task Perform(Inbox inbox)
        {
            entered.SetResult();
            inbox.Receive();
            await resume.Task;
        }
    }

    /// <summary>
    /// Tests that sending one message to a dormant actor transitions it to a scheduled state, and that the actor
    /// will be unscheduled only after it consumes the message and exits <c>Perform()</c>.
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task TestScheduledWithOne()
    {
        var actor = new PausingActor();
        // should be unscheduled in initial state
        Assert.IsFalse(actor.Scheduled);

        // send a message to wake the actor — should immediately be scheduled
        actor.Send(new object());
        Assert.IsTrue(actor.Scheduled);

        await actor.entered.Task;
        // actor is now in Perform() but hasn't yet finished — should still be scheduled
        Assert.IsTrue(actor.Scheduled);
        actor.resume.SetResult();

        // wait for the actor to exit Perform() — should become unscheduled
        await actor.Drain();
        Assert.IsFalse(actor.Scheduled);

        // repeat the test to check that after getting unscheduled, the actor can become scheduled again
        actor.Reset();
        actor.Send(new object());
        await actor.entered.Task;
        Assert.IsTrue(actor.Scheduled);

        actor.resume.SetResult();
        await actor.Drain();
        Assert.IsFalse(actor.Scheduled);
    }

    // [TestMethod]
    // public async Task TestScheduledOnRepeat()
    // {
    //     var actor = new PausingActor();
    //     Assert.IsFalse(actor.Scheduled);

    //     // send the first message and verify scheduled state
    //     actor.Send(new object());
    //     Assert.IsTrue(actor.Scheduled);

    //     await actor.entered.Task;
    //     Assert.IsTrue(actor.Scheduled);
    //     actor.resume.SetResult();

    //     // send the second message
    //     actor.Send(new object());
    // }
}