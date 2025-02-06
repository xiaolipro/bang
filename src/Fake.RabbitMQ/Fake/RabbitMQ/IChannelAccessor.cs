using RabbitMQ.Client;

namespace Fake.RabbitMQ;

public interface IChannelAccessor : IDisposable
{
    /// <summary>
    /// <see cref="Channel"/> 的名称。
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 永远不要手动释放该 <see cref="Channel"/> 对象，而应该在使用后释放 <see cref="IChannelAccessor"/> 实例。
    /// </summary>
    IModel Channel { get; }
    
    /// <summary>
    /// 是否是新创建的 <see cref="Channel"/> 对象。
    /// </summary>
    public bool IsNew { get; }
}