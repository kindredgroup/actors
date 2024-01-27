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

    private const int InboxCapacity = 32;

    private readonly Queue<M> inbox = new Queue<M>(InboxCapacity);

    private readonly object stateLock = new object();

    private bool scheduled = false;

    private readonly ActorContext context;

    private readonly Queue<TaskCompletionSource> drainListeners = new Queue<TaskCompletionSource>(1);

    protected Actor()
    {
        this.context = new ActorContext(this);
    }

    public void Send(M message)
    {
        bool willSchedule;
        lock (stateLock)
        {
            inbox.Enqueue(message);
            if (!scheduled)
            {
                scheduled = true;
                willSchedule = true;
            }
            else
            {
                willSchedule = false;
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
        private Actor<M> parent;

        internal ActorContext(Actor<M> parent)
        {
            this.parent = parent;
        }

        public M Receive()
        {
            lock (parent.stateLock)
            {
                return parent.inbox.Dequeue();
            }
        }

        public List<M> ReceiveAll()
        {
            lock (parent.stateLock)
            {
                var batch = new List<M>(parent.inbox.Count);
                while (parent.inbox.Any())
                {
                    batch.Add(parent.inbox.Dequeue());
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
