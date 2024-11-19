using System;
using System.IO;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Fake.RabbitMQ;

/// <summary>
/// RabbitMQ连接器、Channel工厂
/// </summary>
/// <remarks>复用同一个IConnection，减少连接的高昂开销</remarks>
public class RabbitMqChannelFactory(ILogger<RabbitMqChannelFactory> logger, IRabbitMqConnectionPool connectionPool)
    : IRabbitMqChannelFactory
{
    private readonly object _lock = new();

    private bool _disposed;


    public IModel CreateChannel(string? connectionName)
    {
        KeepAlive(connectionName);

        return connectionPool.Get(connectionName).CreateModel();
    }

    /// <summary>
    /// 保持连接活性
    /// </summary>
    public void KeepAlive(string? connectionName)
    {
        if (connectionPool.Get(connectionName) is { IsOpen: true }) return;

        ConnectRabbitMq(connectionName);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        logger.LogInformation("Channel工厂被销毁，正在释放RabbitMQ连接池。。。");

        try
        {
            connectionPool.Dispose();
        }
        catch (IOException ex)
        {
            logger.LogCritical(ex.ToString());
        }
    }

    /// <summary>
    /// 重连
    /// </summary>
    /// <exception cref="FakeException">RabbitMq connections could not be created and opened</exception>
    private void ConnectRabbitMq(string? connectionName)
    {
        logger.LogInformation($"正在尝试连接客户端  RabbitMQ:ConnectionName:{connectionName}");

        lock (_lock)
        {
            var connection = connectionPool.Get(connectionName);

            if (connection is { IsOpen: false } || _disposed)
            {
                throw new FakeException($"致命错误：无法创建并打开连接 RabbitMQ:ConnectionName:{connectionName}");
            }

            OnConnection(connection, connectionName);

            logger.LogInformation(
                $"成功建立持久连接 RabbitMQ:ConnectionName:{connectionName} -> {connection.Endpoint.HostName}");
        }
    }

    private void OnConnection(IConnection connection, string? connectionName)
    {
        connection.ConnectionShutdown += (_, _) =>
        {
            if (_disposed) return;

            logger.LogWarning($"RabbitMQ:ConnectionName:{connectionName} 连接已被关闭，正在尝试重新连接。。。");

            ConnectRabbitMq(connectionName);
        };
        connection.CallbackException += (_, args) =>
        {
            if (_disposed) return;

            logger.LogWarning($"RabbitMQ:ConnectionName:{connectionName} 连接发生异常：{args.Exception.Message}，正在尝试重新连接。。。");

            ConnectRabbitMq(connectionName);
        };

        // RabbitMQ出于自身保护策略，通过阻塞方式限制写入，导致了生产者应用“假死”，不再对外服务。
        // 比若说CPU/IO/RAM资源下降，队列堆积，导致堵塞，就会触发这个事件
        connection.ConnectionBlocked += (_, _) =>
        {
            if (_disposed) return;

            logger.LogWarning($"RabbitMQ:ConnectionName:{connectionName} 连接被阻塞，正在尝试重新连接。。。");

            ConnectRabbitMq(connectionName);
        };
    }
}