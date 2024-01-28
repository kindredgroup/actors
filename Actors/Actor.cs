namespace Actors;

/// <summary>
/// A unit of serial execution within a broader concurrent topology.
/// 
/// Each actor has a dedicated <c>Inbox</c> to which messages can be posted 
/// in a fire-and-forget fashion via the <c>Send()</c> method. They 
/// are subsequently delivered to the actor's <c>Perform()</c> method in the 
/// order of their submission.
/// </summary>
/// <typeparam name="M">The message type.</typeparam>
public abstract class Actor<M>
{
    /// <summary>
    /// A property indicating that an actor is scheduled, meaning that there
    /// is exactly one bound task that is executing the actor's <c>Perform()</c>
    /// method, and will continue executing until the actor's inbox is drained.
    /// 
    /// Note, an actor may be in a scheduled state even with no pending
    /// messages in its inbox, as long as the <c>Perform()</c> invocation for
    /// the last message has not yet returned.
    /// 
    /// Conversely, an unscheduled state indicates that there are no pending
    /// messages in the actor's inbox and there is no task bound to the actor's
    /// execution.
    /// 
    /// It is an invariant of the actor's state that, if there is at least one
    /// message in its inbox, then it must be scheduled. Under the hood, it is
    /// the caller that loads the first message into the inbox for an
    /// unscheduled actor that also transitions the actor to the scheduled state 
    /// and binds a task to its execution.
    /// </summary>
    public bool Scheduled
    {
        get
        {
            lock (stateLock)
            {
                return scheduled;
            }
        }
    }

    private const int DefaultInitialInboxCapacity = 32;

    private readonly object stateLock = new object();

    private bool scheduled = false;

    private readonly Inbox inbox;

    private readonly Queue<TaskCompletionSource> drainListeners = new Queue<TaskCompletionSource>(1);

    protected Actor() : this(DefaultInitialInboxCapacity) { }

    protected Actor(int initialInboxCapacity)
    {
        this.inbox = new Inbox(this, initialInboxCapacity);
    }

    /// <summary>
    /// Posts a message to the actor's inbox in a fire-and-forget fashion. This
    /// method is guaranteed not to block.
    /// </summary>
    /// <param name="message">The message to send to the actor.</param>
    public void Send(M message)
    {
        bool willSchedule;
        lock (stateLock)
        {
            inbox.messages.Enqueue(message);
            willSchedule = !scheduled;
            if (willSchedule)
            {
                scheduled = true;
            }
        }

        if (willSchedule)
        {
            Task.Run(RunAsync);
        }
    }

    /// <summary>
    /// Obtains a task that is completed when this actor is fully drained. Drainage is the
    /// process of emptying the actor's inbox to the point that the actor has no pending
    /// messages and is eventually unscheduled.
    /// 
    /// An actor's drain state is not permanent. The completion of the returned task
    /// only indicates that the actor was unscheduled at some point after <c>Drain()</c>
    /// was called; it does not preclude the actor from taking on additional messages.
    /// 
    /// Calling this method on an actor that is already unscheduled has the effect of
    /// returning a completed task.
    /// </summary>
    /// <returns>A <c>Task</c>.</returns>
    public Task Drain()
    {
        lock (stateLock)
        {
            if (scheduled)
            {
                var completion = new TaskCompletionSource();
                drainListeners.Enqueue(completion);
                return completion.Task;
            }
            else
            {
                return Task.CompletedTask;
            }
        }
    }

    private async Task RunAsync()
    {
        while (true)
        {
            try
            {
                await Perform(inbox);
            }
            catch (Exception e)
            {
                OnError(e);
            }

            bool hasBacklog;
            lock (stateLock)
            {
                hasBacklog = inbox.messages.Any();
                if (!hasBacklog)
                {
                    scheduled = false;
                    while (drainListeners.Any())
                    {
                        var drainListener = drainListeners.Dequeue();
                        drainListener.SetResult();
                    }
                }
            }

            if (!hasBacklog)
            {
                return;
            }
        }
    }

    /// <summary>
    /// Represents the actor's dedicated inbox.
    /// </summary>
    public class Inbox
    {
        private readonly Actor<M> actor;

        internal readonly Queue<M> messages;

        internal Inbox(Actor<M> actor, int initialInboxCapacity)
        {
            this.actor = actor;
            this.messages = new Queue<M>(initialInboxCapacity);
        }

        /// <summary>
        /// Receives the oldest pending message, removing it from the head
        /// of the inbox queue.
        /// </summary>
        /// <returns>The received message.</returns>
        public M Receive()
        {
            lock (actor.stateLock)
            {
                return messages.Dequeue();
            }
        }

        /// <summary>
        /// Receives all pending messages in one operation, moving them from the inbox
        /// queue to a temporary list. The oldest message will appear towards the front of
        /// the returned list.
        /// </summary>
        /// <returns>A list of pending messages.</returns>
        public List<M> ReceiveAll()
        {
            lock (actor.stateLock)
            {
                var batch = new List<M>(messages.Count);
                while (messages.Any())
                {
                    batch.Add(messages.Dequeue());
                }
                return batch;
            }
        }
    }

    /// <summary>
    /// Implemented by actors to perform their designated role when at least one 
    /// message accumulates in their inbox.
    /// </summary>
    /// <param name="inbox">The actor's inbox.</param>
    /// <returns>A task awaited by the actor's executor.</returns>
    protected abstract Task Perform(Inbox inbox);

    /// <summary>
    /// Handles uncaught exceptions propagated out of <c>Perform()</c>. The default
    /// implementation logs to <c>stderr</c>. Actor implementations should
    /// override this method with a more suitable error handling approach.
    /// </summary>
    /// <param name="e">The uncaught exception./param>
    protected virtual void OnError(Exception e)
    {
        Console.Error.WriteLine("Error in {0}: {1}", this.GetType().FullName, e);
    }
}

/// <summary>
/// An actor specialization that expects messages of type <c>object</c> and most
/// likely handles multiple concrete message types.
/// </summary>
public abstract class Actor : Actor<object>
{
    protected Actor() : base() { }

    protected Actor(int initialInboxCapacity) : base(initialInboxCapacity) { }
}
