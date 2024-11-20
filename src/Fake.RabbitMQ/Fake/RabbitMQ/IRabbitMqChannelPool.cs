using System;
using RabbitMQ.Client;

namespace Fake.RabbitMQ;

public interface IRabbitMqChannelPool : IDisposable
{
    /// <summary>
    /// 申请一个 <see cref="IChannelAccessor"/> 实例，用于访问 RabbitMQ 的 <see cref="IModel"/> 对象。
    /// 使用using语句可自动归还 <see cref="IModel"/>，请勿手动释放 <see cref="IModel"/>。
    /// </summary>
    /// <param name="channelName">通道名称</param>
    /// <param name="connectionName">连接名称</param>
    /// <param name="configureChannel">初始化通道</param>
    /// <returns></returns>
    IChannelAccessor Acquire(string? channelName = null, string? connectionName = null,
        Action<IModel>? configureChannel = null);
}