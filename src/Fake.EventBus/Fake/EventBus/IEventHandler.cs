using System.Threading.Tasks;

namespace Fake.EventBus;

/// <summary>
/// 事件处理器
/// </summary>
/// <typeparam name="TEvent">事件</typeparam>
public abstract class EventHandler<TEvent> : IEventHandler where TEvent : Event
{
    /// <summary>
    /// 处理事件
    /// </summary>
    /// <param name="event">事件携带的数据</param>
    /// <returns></returns>
    public abstract Task HandleAsync(TEvent @event);

    public Task HandleAsync(Event @event) => HandleAsync((TEvent)@event);
}

public interface IEventHandler
{
    Task HandleAsync(Event @event);
}