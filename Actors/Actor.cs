namespace Actors;

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

    private readonly Queue<M> inbox;

    private readonly object stateLock = new object();

    private bool scheduled = false;

    private readonly ActorContext context;

    private readonly Queue<TaskCompletionSource> drainListeners = new Queue<TaskCompletionSource>(1);

    protected Actor() : this(DefaultInitialInboxCapacity) { }

    protected Actor(int initialInboxCapacity)
    {
        this.inbox = new Queue<M>(DefaultInitialInboxCapacity);
        this.context = new ActorContext(this);
    }

    public void Send(M message)
    {
        bool willSchedule;
        lock (stateLock)
        {
            inbox.Enqueue(message);
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
                await Perform(context);
            }
            catch (Exception e)
            {
                OnError(e);
            }

            bool hasBacklog;
            lock (stateLock)
            {
                hasBacklog = inbox.Any();
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

    public class ActorContext
    {
        private readonly Actor<M> actor;

        internal ActorContext(Actor<M> actor)
        {
            this.actor = actor;
        }

        public M Receive()
        {
            lock (actor.stateLock)
            {
                return actor.inbox.Dequeue();
            }
        }

        public List<M> ReceiveAll()
        {
            lock (actor.stateLock)
            {
                var batch = new List<M>(actor.inbox.Count);
                while (actor.inbox.Any())
                {
                    batch.Add(actor.inbox.Dequeue());
                }
                return batch;
            }
        }
    }

    protected abstract Task Perform(ActorContext context);

    protected virtual void OnError(Exception e)
    {
        Console.Error.WriteLine("Error in {0}: {1}", this.GetType().FullName, e);
    }
}

public abstract class Actor : Actor<object>;
