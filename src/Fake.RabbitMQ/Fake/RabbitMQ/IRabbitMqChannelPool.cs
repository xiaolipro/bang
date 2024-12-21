using System;
using RabbitMQ.Client;

namespace Fake.RabbitMQ;

public interface IRabbitMqChannelPool : IDisposable
{
    /// <summary>
    /// 申请一个 <see cref="IChannelAccessor"/> 实例，用于访问 RabbitMQ 的 <see cref="IModel"/> 对象。
    /// 使用using语句可自动归还 <see cref="IModel"/> 的使用权（线程安全的），请勿手动释放 <see cref="IModel"/>, 可能会造成后续的使用异常。
    /// </summary>
    /// <param name="channelName">通道名称</param>
    /// <param name="connectionName">连接名称</param>
    /// <returns></returns>
    IChannelAccessor Acquire(string channelName = "", string? connectionName = null);
    
    /// <summary>
    /// 释放 <see cref="IModel"/> 对象，从池中移除。
    /// </summary>
    /// <param name="channelName"></param>
    /// <param name="connectionName"></param>
    /// <returns></returns>
    bool Release(string channelName = "", string? connectionName = null);
}