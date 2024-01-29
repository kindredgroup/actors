namespace Actors.Tests;

public abstract class ErrorTrappingActor<M> : Actor<M>
{
    public Exception? Exception { get; private set; }

    protected override void OnError(Exception e)
    {
        Exception = e;
        base.OnError(e);
    }

    public void AssertNoError()
    {
        if (Exception is not null)
        {
            throw new AssertFailedException("An unexpected exception was caught.", Exception);
        }
    }
}