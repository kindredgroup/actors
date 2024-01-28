namespace Actors;

/// <summary>
/// A unit of serial execution within a broader concurrent topology.
/// 
/// Each actor has a dedicated inbox to which messages can be posted in a fire-and-forget fashion via the <c>Send()</c> method. They 
/// are subsequently delivered to the actor's <c>Perform()</c> method in the order of their submission.
/// </summary>
/// <typeparam name="M"></typeparam>
public abstract class Actor<M>
{
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

    public class Inbox
    {
        private readonly Actor<M> actor;

        internal readonly Queue<M> messages;

        internal Inbox(Actor<M> actor, int initialInboxCapacity)
        {
            this.actor = actor;
            this.messages = new Queue<M>(initialInboxCapacity);
        }

        public M Receive()
        {
            lock (actor.stateLock)
            {
                return messages.Dequeue();
            }
        }

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

    protected abstract Task Perform(Inbox inbox);

    protected virtual void OnError(Exception e)
    {
        Console.Error.WriteLine("Error in {0}: {1}", this.GetType().FullName, e);
    }
}

public abstract class Actor : Actor<object>;
