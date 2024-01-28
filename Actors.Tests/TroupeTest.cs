namespace Actors.Tests;

static class IEnumerableExtensions
{
    public static void AssertNoError(this PausingActor[] actors)
    {
        foreach (var actor in actors)
        {
            actor.AssertNoError();
        }
    }
}

[TestClass]
public class TroupeTest
{
    [TestMethod]
    public void TestMembers()
    {
        PausingActor[] actors = [new PausingActor(), new PausingActor()];
        var troupe = Troupe.Of(actors);
        CollectionAssert.AreEqual(actors, troupe.Members);
    }

    [TestMethod]
    public async Task TestDrainAny()
    {
        PausingActor[] actors = [new PausingActor(), new PausingActor()];
        var troupe = Troupe.Of(actors);

        var m0 = new Barrier();
        var m1 = new Barrier();

        actors[0].Send(m0);
        actors[1].Send(m1);

        var drain = troupe.DrainAny();

        // no drain task is completed if all actors are still in Perform()
        await m0.Entered.Task;
        await m1.Entered.Task;
        Assert.IsTrue(actors[0].Scheduled);
        Assert.IsTrue(actors[1].Scheduled);
        Assert.IsFalse(drain.IsCompleted);

        // allow one actor to complete and await drainage
        m0.Resume.SetResult();
        await drain;
        Assert.IsFalse(actors[0].Scheduled);

        // complete the second actor and let it drain
        m1.Resume.SetResult();
        await actors[1].Drain();
        Assert.IsFalse(actors[1].Scheduled);

        actors.AssertNoError();
    }
    [TestMethod]
    public async Task TestDrainAll()
    {
        PausingActor[] actors = [new PausingActor(), new PausingActor()];
        var troupe = Troupe.Of(actors);

        var m0 = new Barrier();
        var m1 = new Barrier();

        actors[0].Send(m0);
        actors[1].Send(m1);

        var drain = troupe.DrainAll();

        // no drain task is completed if all actors are still in Perform()
        await m0.Entered.Task;
        await m1.Entered.Task;
        Assert.IsTrue(actors[0].Scheduled);
        Assert.IsTrue(actors[1].Scheduled);
        Assert.IsFalse(drain.IsCompleted);

        // allow one actor to complete and drain it... troupe drain will still not be completed
        m0.Resume.SetResult();
        await actors[0].Drain();
        Assert.IsFalse(actors[0].Scheduled);
        Assert.IsFalse(drain.IsCompleted);

        // complete the second actor and await troupe drainage
        m1.Resume.SetResult();
        await drain;
        Assert.IsFalse(actors[1].Scheduled);
        
        actors.AssertNoError();
    }
}