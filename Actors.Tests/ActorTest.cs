using System.Diagnostics;

namespace Actors.Tests;

[TestClass]
public class ActorTest
{
    /// <summary>
    /// Tests that sending one message to a dormant actor transitions it to a scheduled state, and that the actor
    /// will be unscheduled only after it consumes the message and exits <c>Perform()</c>.
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task TestScheduledOnFirst()
    {
        var actor = new PausingActor();

        // should be unscheduled in initial state
        Assert.IsFalse(actor.Scheduled);

        // send a message to wake the actor — should immediately be scheduled
        var m1 = new Barrier();
        actor.Send(m1);
        Assert.IsTrue(actor.Scheduled);

        await m1.Entered.Task;
        // actor is now in Perform() but hasn't yet finished — should still be scheduled
        Assert.IsTrue(actor.Scheduled);
        m1.Resume.SetResult();

        // wait for the actor to exit Perform() — should become unscheduled
        await actor.Drain();
        Assert.IsFalse(actor.Scheduled);

        // repeat the test to check that after getting unscheduled, the actor can become scheduled again
        var m2 = new Barrier();
        actor.Send(m2);
        await m2.Entered.Task;
        Assert.IsTrue(actor.Scheduled);

        m2.Resume.SetResult();
        await actor.Drain();
        Assert.IsFalse(actor.Scheduled);

        actor.AssertNoError();
    }

    /// <summary>
    /// Tests that posting a second message into an empty inbox while the first is still being processed, 
    /// retains the scheduled state.
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task TestScheduledOnRepeat()
    {
        var actor = new PausingActor();
        Assert.IsFalse(actor.Scheduled);

        // send the first message and verify scheduled state
        var m1 = new Barrier();
        actor.Send(m1);
        Assert.IsTrue(actor.Scheduled);

        await m1.Entered.Task;
        Assert.IsTrue(actor.Scheduled);

        // send the second message while actor is still in Perform()
        var m2 = new Barrier();
        actor.Send(m2);
        Assert.IsTrue(actor.Scheduled);

        // upon completion of m1, the actor should remain scheduled
        m1.Resume.SetResult();
        Assert.IsTrue(actor.Scheduled);

        // wait for m2's evaluation to start — the actor is still scheduled
        await m2.Entered.Task;
        Assert.IsTrue(actor.Scheduled);

        // after m2 completes, the actor should eventually become unscheduled
        m2.Resume.SetResult();
        await actor.Drain();
        Assert.IsFalse(actor.Scheduled);

        actor.AssertNoError();
    }

    class FaultyActor : ErrorTrappingActor<object>
    {
        protected override Task Perform(Inbox inbox)
        {
            inbox.Receive();
            throw new InvalidOperationException();
        }
    }

    [TestMethod]
    public async Task TestUncaughtExceptionHandling()
    {
        var actor = new FaultyActor();
        actor.AssertNoError();
        actor.Send(new object());
        await actor.Drain();
        Assert.IsInstanceOfType(actor.Exception, typeof(InvalidOperationException));
    }

    class BatchActor : ErrorTrappingActor<int>
    {
        internal TaskCompletionSource Resume { get; } = new();

        protected override async Task Perform(Inbox inbox)
        {
            await Resume.Task;
            var items = inbox.ReceiveAll();
            CollectionAssert.AreEqual(new List<int>{0, 1, 2}, items);

            // second ReceiveAll() should produce no results since no more messages were added
            Assert.AreEqual(0, inbox.ReceiveAll().Count);

            // actors can send messages to themselves
            Send(10);
            Send(20);
            Send(30);

            Assert.AreEqual(10, inbox.Receive());
            CollectionAssert.AreEqual(new List<int>{20, 30}, inbox.ReceiveAll());
        }
    }

    [TestMethod]
    public async Task TestReceiveAll()
    {
        var actor = new BatchActor();
        for (int i = 0; i < 3; i++)
        {
            actor.Send(i);
        }
        actor.Resume.SetResult();
        await actor.Drain();
        actor.AssertNoError();
    }
}