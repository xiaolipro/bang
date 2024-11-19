using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace Fake.RabbitMQ;

public class FakeRabbitMqOptions
{
    /// <summary>
    /// RabbitMQ默认连接
    /// </summary>
    public string DefaultConnectionName { get; set; } = "Default";
    
    /// <summary>
    /// Channel池销毁等待时间
    /// </summary>
    public TimeSpan ChannelPoolDisposeDuration { get; set; } = TimeSpan.FromSeconds(10);

    public Dictionary<string, ConnectionFactory> Connections { get; set; }

    public ConnectionFactory Default
    {
        get => Connections[DefaultConnectionName];
        private set => Connections[DefaultConnectionName] = value;
    }

    public FakeRabbitMqOptions()
    {
        Default = new ConnectionFactory();
        Connections = new();
    }

    public ConnectionFactory GetOrDefault(string connectionName)
    {
        return Connections.TryGetValue(connectionName, out var connectionFactory) ? connectionFactory : Default;
    }
}