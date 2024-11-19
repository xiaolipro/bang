using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Fake.RabbitMQ;

public class RabbitMqConnectionPool(IOptions<FakeRabbitMqOptions> options, ILogger<RabbitMqConnectionPool> logger)
    : IRabbitMqConnectionPool
{
    private readonly FakeRabbitMqOptions _options = options.Value;
    protected ConcurrentDictionary<string, Lazy<IConnection>> Connections { get; } = new();

    private bool _isDisposed;

    public IConnection Get(string? connectionName = null)
    {
        connectionName ??= _options.DefaultConnectionName;

        try
        {
            var lazyConnection = Connections.GetOrAdd(
                connectionName, v => new Lazy<IConnection>(() =>
                    {
                        var connectionFactory = _options.GetOrDefault(v);
                        // 处理集群
                        var hostnames = connectionFactory.HostName.TrimEnd(';').Split(';');
                        return hostnames.Length == 1
                            ? connectionFactory.CreateConnection()
                            : connectionFactory.CreateConnection(hostnames);
                    })
            );

            return lazyConnection.Value;
        }
        catch (Exception)
        {
            Connections.TryRemove(connectionName, out _);
            throw;
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        foreach (var connection in Connections.Values)
        {
            try
            {
                connection.Value.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to dispose RabbitMQ connection.");
            }
        }

        Connections.Clear();
    }
}