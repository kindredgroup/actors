namespace Actors;

/// <summary>
/// A grouping of actors for composing drain requests.
/// </summary>
/// <typeparam name="M">The message type.</typeparam>
public sealed class Troupe<M>
{
    /// <summary>
    /// The actors in the troupe.
    /// </summary>
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

    /// <summary>
    /// Obtains a task that is completed when all actors in the troupe have been drained.
    /// </summary>
    /// <returns>A <c>Task</c>.</returns>
    public Task DrainAll()
    {
        return Task.WhenAll(DrainTasks());
    }

    /// <summary>
    /// Obtains a task that is completed when at least one actor in the troupe has been drained.
    /// </summary>
    /// <returns>A <c>Task</c>.</returns>
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