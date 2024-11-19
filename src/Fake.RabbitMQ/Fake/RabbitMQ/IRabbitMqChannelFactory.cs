using System;
using RabbitMQ.Client;

namespace Fake.RabbitMQ;

/// <summary>
/// RabbitMQ--Channel工厂
/// </summary>
public interface IRabbitMqChannelFactory : IDisposable
{
    /// <summary>
    /// 保持连接活性
    /// </summary>
    void KeepAlive(string? connectionName);

    /// <summary>
    /// 创建channel，channel不是线程安全的
    /// </summary>
    /// <returns></returns>
    IModel CreateChannel(string? connectionName);
}