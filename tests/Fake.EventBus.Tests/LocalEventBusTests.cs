using Fake.EventBus.Local;
using Fake.EventBus.Tests.Events;
using Fake.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Fake.EventBus.Tests;

public class LocalEventBusTests : ApplicationTest<FakeEventBusTestModule>
{
    private readonly ILocalEventBus _eventBus;

    public LocalEventBusTests()
    {
        SimpleLocalEventHandler.Init();
        _eventBus = ServiceProvider.GetRequiredService<ILocalEventBus>();
    }

    [Fact]
    public async Task 发布本地事件无须订阅()
    {
        await _eventBus.PublishAsync(new SimpleEvent(1));
        await _eventBus.PublishAsync(new SimpleEvent(1));
        await _eventBus.PublishAsync(new SimpleEvent(1));

        Assert.Equal(3, SimpleLocalEventHandler.HandleCount);
    }

    [Fact]
    public async Task 事件处理者可以释放()
    {
        await _eventBus.PublishAsync(new SimpleEvent(1));
        await _eventBus.PublishAsync(new SimpleEvent(1));

        Assert.Equal(2, SimpleLocalEventHandler.DisposedCount);
    }
}