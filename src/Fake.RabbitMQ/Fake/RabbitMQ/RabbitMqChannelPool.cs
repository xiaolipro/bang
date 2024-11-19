using System;
using System.Collections.Concurrent;
using System.Threading;
using Fake.Timing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Fake.RabbitMQ;

public class RabbitMqChannelPool(
    IRabbitMqChannelFactory rabbitMqChannelFactory,
    ILogger<RabbitMqChannelPool> logger,
    IFakeClock clock,
    IOptions<FakeRabbitMqOptions> options) : IRabbitMqChannelPool
{
    private bool _isDisposed;
    protected ConcurrentDictionary<string, ChannelWrapper> Channels { get; } = new();

    public virtual IChannelAccessor Acquire(string? channelName = null, string? connectionName = null)
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(RabbitMqChannelPool));
        }

        channelName ??= connectionName ?? options.Value.DefaultConnectionName;

        var wrapper = Channels.GetOrAdd(
            channelName,
            _ => new ChannelWrapper(rabbitMqChannelFactory.CreateChannel(connectionName))
        );
        
        wrapper.Acquire();
        
        return new ChannelAccessor(channelName, wrapper);
    }

    public virtual void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;

        var cost = clock.MeasureExecutionTime(DoDispose);
        logger.LogInformation($"释放Channel[{Channels.Count}]池完成，耗时：" + cost);

        Channels.Clear();
        rabbitMqChannelFactory.Dispose();
    }

    private void DoDispose()
    {
        logger.LogInformation($"准备释放{Channels.Count}个Channel");

        var remainingTime = options.Value.ChannelPoolDisposeDuration;

        foreach (var channelWrapper in Channels.Values)
        {
            var itemTime = clock.MeasureExecutionTime(timeout =>
                {
                    try
                    {
                        channelWrapper.Dispose((TimeSpan)timeout);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning("释放Channel失败" + ex);
                    }
                }, remainingTime);

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
                while (_isInUse) // race
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