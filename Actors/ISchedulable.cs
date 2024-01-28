namespace Actors;

public interface ISchedulable
{
    bool Scheduled { get; }

    Task Drain();
}