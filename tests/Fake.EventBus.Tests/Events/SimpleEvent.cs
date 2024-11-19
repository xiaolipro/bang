namespace Fake.EventBus.Tests.Events;

public class SimpleEvent(int num) : Event
{
    public int Num { get; } = num;
}