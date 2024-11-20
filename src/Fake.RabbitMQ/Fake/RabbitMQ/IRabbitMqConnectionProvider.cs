using RabbitMQ.Client;

namespace Fake.RabbitMQ;

/// <summary>
/// RabbitMQ连接供应商
/// </summary>
public interface IRabbitMqConnectionProvider : IDisposable
{
    IConnection Get(string? connectionName = null, Action<ConnectionFactory>? configure = null);
}