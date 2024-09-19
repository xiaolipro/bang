using System;
using System.Collections.Concurrent;
using System.IO;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Fake.RabbitMQ;

public class RabbitMqConnectionPool : IRabbitMqConnectionPool
{
    private readonly FakeRabbitMqOptions _options;
    protected ConcurrentDictionary<string, Lazy<IConnection>> Connections { get; }

    private bool _isDisposed;

    public RabbitMqConnectionPool(IOptions<FakeRabbitMqOptions> options)
    {
        _options = options.Value;
        Connections = new ConcurrentDictionary<string, Lazy<IConnection>>();
    }

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
            catch
            {
                // ignored
            }
        }

        Connections.Clear();
    }
}