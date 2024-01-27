namespace Actors;

public abstract class ActorBase<M>
{
    private const int InboxCapacity = 32;

    private Queue<M> inbox = new Queue<M>(InboxCapacity);

    private object inboxLock = new object();

    private bool scheduled = false;

    private ActorContext context;

    protected ActorBase()
    {
        this.context = new ActorContext(this);
    }

    public void Send(M message)
    {
        bool willSchedule;
        lock (inboxLock)
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

    private async Task RunAsync()
    {
        while (true)
        {
            await Perform(context);

            bool hasBacklog;
            lock (inboxLock)
            {
                hasBacklog = inbox.Any();
                if (!hasBacklog)
                {
                    scheduled = false;
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
        private ActorBase<M> parent;

        internal ActorContext(ActorBase<M> parent)
        {
            this.parent = parent;
        }

        public M Receive()
        {
            lock (parent.inboxLock)
            {
                return parent.inbox.Dequeue();
            }
        }
    }

    protected abstract Task Perform(ActorContext context);
}

public abstract class ActorBase : ActorBase<object>;
