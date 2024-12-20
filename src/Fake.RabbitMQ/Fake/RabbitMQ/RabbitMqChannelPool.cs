using System.Collections.Concurrent;
using Fake.Timing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Fake.RabbitMQ;

public class RabbitMqChannelPool(
    IRabbitMqConnectionPool rabbitMqConnectionPool,
    ILogger<RabbitMqChannelPool> logger,
    IFakeClock clock,
    IOptions<FakeRabbitMqOptions> options) : IRabbitMqChannelPool
{
    private bool _isDisposed;
    protected ConcurrentDictionary<string, ChannelWrapper> Channels { get; } = new();
    
    public virtual IChannelAccessor Acquire(string? channelName = null, string? connectionName = null,
        Action<IModel>? configureChannel = null)
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(RabbitMqChannelPool));
        }

        channelName ??= connectionName ?? options.Value.DefaultConnectionName;

        var wrapper = Channels.GetOrAdd(
            $"{connectionName}_{channelName}",
            _ =>
                {
                    var wrapper = new ChannelWrapper(rabbitMqConnectionPool.Get(connectionName).CreateModel());
                    configureChannel?.Invoke(wrapper.Channel);
                    return wrapper;
                }
        );
        
        // 与 Connection 不同，Channel 对象在 RabbitMQ 中是非线程安全的，因此需要加锁
        wrapper.Acquire();

        return new ChannelAccessor(channelName, wrapper);
    }

    public virtual bool Release(string channelName, string? connectionName = null)
    {
        var key = $"{connectionName}_{channelName}";
        if (Channels.TryGetValue(key, out var wrapper))
        {
            wrapper.Channel.Dispose();
            Channels.TryRemove(key, out _);
            return true;
        }

        return false;
    }

    public virtual void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;

        var cost = clock.MeasureExecutionTime(DoDispose);
        logger.LogInformation("Disposed Channel pool ({0} channels) in {1}ms", Channels.Count, cost.TotalMilliseconds);

        Channels.Clear();
    }

    private void DoDispose()
    {
        logger.LogInformation("Disposing Channel pool ({0} channels)", Channels.Count);

        var remainingTime = options.Value.ChannelPoolDisposeDuration;

        foreach (var channelWrapper in Channels.Values)
        {
            var timeout = remainingTime;
            var itemTime = clock.MeasureExecutionTime(() =>
                {
                    try
                    {
                        channelWrapper.Dispose(timeout);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning("Dispose channel error: {0}", ex.Message);
                    }
                });

            remainingTime = remainingTime > itemTime ? remainingTime - itemTime : TimeSpan.Zero;
        }
    }

    protected class ChannelAccessor(string name, ChannelWrapper channelWrapper) : IChannelAccessor
    {
        public string Name { get; } = name;
        public IModel Channel { get; } = channelWrapper.Channel;

        public void Dispose()
        {
            channelWrapper.Release();
        }
    }


    protected class ChannelWrapper(IModel channel)
    {
        public IModel Channel { get; } = channel;

        private volatile bool _isInUse;

        public void Acquire()
        {
            lock (this)
            {
                while (_isInUse) // race, only one can use it
                {
                    // 释放锁，等待其他线程调用 Release 方法
                    Monitor.Wait(this);
                }

                _isInUse = true;
            }
        }

        public void Dispose(TimeSpan timeout)
        {
            lock (this)
            {
                if (!_isInUse)
                {
                    return;
                }

                Monitor.Wait(this, timeout);
            }

            Channel.Dispose();
        }

        public void Release()
        {
            lock (this)
            {
                _isInUse = false;
                Monitor.PulseAll(this); // 通知其他wait线程 release了
            }
        }
    }
}