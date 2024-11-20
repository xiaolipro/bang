namespace Fake.EventBus.Distributed;

/// <summary>
/// 集成事件
/// </summary>
[Serializable]
public class IntegrationEvent : Event
{
    public override string ToString()
    {
        return $"[集成事件：{GetType().Name} Id：{Id} 创建时间：{CreationTime}]";
    }
}