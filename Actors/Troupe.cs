namespace Actors;

/// <summary>
/// A grouping of schedulable instances for composing drain requests.
/// </summary>
public sealed class Troupe
{
    /// <summary>
    /// The members of the troupe.
    /// </summary>
    public List<ISchedulable> Members
    {
        get
        {
            return members;
        }
    }

    private readonly List<ISchedulable> members;

    private Troupe(List<ISchedulable> members)
    {
        this.members = members;
    }

    /// <summary>
    /// Obtains a task that is completed when all members of the troupe have been drained of 
    /// their backlogs and unscheduled.
    /// </summary>
    /// <returns>A <c>Task</c>.</returns>
    public Task DrainAll()
    {
        return Task.WhenAll(DrainTasks());
    }

    /// <summary>
    /// Obtains a task that is completed when at least one member of the troupe has been drained 
    /// of its backlog and unscheduled.
    /// </summary>
    /// <returns>A <c>Task</c>.</returns>
    public Task DrainAny()
    {
        return Task.WhenAny(DrainTasks());
    }

    private IEnumerable<Task> DrainTasks()
    {
        return members.Select(member => member.Drain());
    }

    public static Troupe Of(IEnumerable<ISchedulable> members)
    {
        return new Troupe(members.ToList());
    }        
    
    public static Troupe OfNullable(IEnumerable<ISchedulable?> members)
    {
        return Of(members.Where(member => member is not null).Select(member => member!));
    }
}