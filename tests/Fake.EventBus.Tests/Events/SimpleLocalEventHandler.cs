using Fake.DependencyInjection;

namespace Fake.EventBus.Tests.Events;

public class SimpleLocalEventHandler : EventHandler<SimpleEvent>, ITransientDependency, IDisposable
{
    public static int HandleCount { get; set; }
    public static int DisposedCount { get; set; }

    public static void Init()
    {
        HandleCount = 0;
        DisposedCount = 0;
    }

    public override Task HandleAsync(SimpleEvent @event)
    {
        HandleCount++;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        DisposedCount++;
    }
}