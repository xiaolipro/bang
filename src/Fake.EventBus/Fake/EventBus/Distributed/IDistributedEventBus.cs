namespace Fake.EventBus.Distributed;

public interface IDistributedEventBus : IEventBus
{
    /// <summary>
    /// 发布事件
    /// </summary>
    /// <param name="event">事件</param>
    Task PublishAsync(IntegrationEvent @event);

    Task IEventBus.PublishAsync(Event @event) => PublishAsync((IntegrationEvent)@event);
}