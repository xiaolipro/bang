using Fake.EventBus;

namespace Fake.Domain.Events;

/// <summary>
/// 领域事件
/// </summary>
[Serializable]
public class DomainEvent : Event
{
    public override string ToString()
    {
        return $"[领域事件：{GetType().Name} Id：{Id} 创建时间：{CreationTime}]";
    }
}