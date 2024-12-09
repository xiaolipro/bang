namespace Fake.EventBus;

[Serializable]
public abstract class Event
{
    /// <summary>
    /// 事件Id
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// 事件创建时间
    /// </summary>
    public virtual DateTime CreationTime { get; } = DateTime.Now;

    public override string ToString()
    {
        return $"[事件：{GetType().Name} Id：{Id} 创建时间：{CreationTime}]";
    }
}