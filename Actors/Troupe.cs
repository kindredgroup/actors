namespace Actors;

public sealed class Troupe<M>
{
    public List<Actor<M>> Actors
    {
        get
        {
            return actors;
        }
    }

    private readonly List<Actor<M>> actors;

    private Troupe(List<Actor<M>> actors)
    {
        this.actors = actors;
    }

    public Task DrainAll()
    {
        return Task.WhenAll(DrainTasks());
    }

    public Task DrainAny()
    {
        return Task.WhenAny(DrainTasks());
    }

    private Task[] DrainTasks()
    {
        return actors.Select(actor => actor.Drain()).ToArray();
    }

    public static Troupe<M> Of(IEnumerable<Actor<M>> actors)
    {
        return new Troupe<M>(actors.ToList());
    }        
    
    public static Troupe<M> OfNullable(IEnumerable<Actor<M>?> actors)
    {
        return Troupe<M>.Of(actors.Where(actor => actor is not null).Select(actor => actor!));
    }
}