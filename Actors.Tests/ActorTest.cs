namespace Actors.Tests;

[TestClass]
public class ActorTest
{
    class Barrier
    {
        internal TaskCompletionSource entered = new();
        internal TaskCompletionSource resume = new();
    }

    class PausingActor : Actor<Barrier>
    {       
        protected override async Task Perform(Inbox inbox)
        {
            var blocker = inbox.Receive();
            blocker.entered.SetResult();
            await blocker.resume.Task;
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

        // initial test to transition from unscheduled to scheduled and back
        {
            // should be unscheduled in initial state
            Assert.IsFalse(actor.Scheduled);

            // send a message to wake the actor — should immediately be scheduled
            var barrier = new Barrier();
            actor.Send(barrier);
            Assert.IsTrue(actor.Scheduled);

            await barrier.entered.Task;
            // actor is now in Perform() but hasn't yet finished — should still be scheduled
            Assert.IsTrue(actor.Scheduled);
            barrier.resume.SetResult();

            // wait for the actor to exit Perform() — should become unscheduled
            await actor.Drain();
            Assert.IsFalse(actor.Scheduled);
        }

        // repeat the test to check that after getting unscheduled, the actor can become scheduled again
        {
            var barrier = new Barrier();
            actor.Send(barrier);
            await barrier.entered.Task;
            Assert.IsTrue(actor.Scheduled);

            barrier.resume.SetResult();
            await actor.Drain();
            Assert.IsFalse(actor.Scheduled);
        }
    }

    [TestMethod]
    public async Task TestScheduledOnRepeat()
    {
        var actor = new PausingActor();
        Assert.IsFalse(actor.Scheduled);

        // send the first message and verify scheduled state
        var b1 = new Barrier();
        actor.Send(b1);
        Assert.IsTrue(actor.Scheduled);

        await b1.entered.Task;
        Assert.IsTrue(actor.Scheduled);

        // send the second message while actor is still in Perform()
        var b2 = new Barrier();
        actor.Send(b2);
        Assert.IsTrue(actor.Scheduled);

        // upon completion of b1, the actor should remain scheduled
        b1.resume.SetResult();
        Assert.IsTrue(actor.Scheduled);

        // wait for b2's evaluation to start — the actor is still scheduled
        await b2.entered.Task;
        Assert.IsTrue(actor.Scheduled);

        // after b2 completes, the actor should eventually become unscheduled
        b2.resume.SetResult();
        await actor.Drain();
        Assert.IsFalse(actor.Scheduled);
    }
}