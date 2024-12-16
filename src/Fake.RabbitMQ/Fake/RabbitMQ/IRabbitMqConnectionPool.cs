using RabbitMQ.Client;

namespace Fake.RabbitMQ;

/// <summary>
/// RabbitMQ连接供应商
/// </summary>
public interface IRabbitMqConnectionPool : IDisposable
{
    IConnection Get(string? connectionName = null);
}