using System;

namespace Fake.RabbitMQ;

public interface IRabbitMqChannelPool : IDisposable
{
    IChannelAccessor Acquire(string? channelName = null, string? connectionName = null);
}