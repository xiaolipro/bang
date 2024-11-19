using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json;
using Fake.EventBus;

namespace Fake.EntityFrameworkCore.IntegrationEventLog;

public class IntegrationEventLogEntry
{
    private IntegrationEventLogEntry()
    {
    }

    public IntegrationEventLogEntry(Event @event, Guid transactionId)
    {
        EventId = @event.Id;
        CreationTime = @event.CreationTime;
        EventTypeName = @event.GetType().FullName ?? String.Empty;
        Content = JsonSerializer.Serialize(@event);
        State = EventStateEnum.NotPublished;
        TimesSent = 0;
        TransactionId = transactionId.ToString();
    }

    public IntegrationEventLogEntry(string transactionId)
    {
        TransactionId = transactionId;
    }

    public Guid EventId { get; private set; }

    public string EventTypeName { get; private set; } = default!;

    /// <summary>
    /// 事件状态
    /// </summary>
    public EventStateEnum State { get; private set; }

    /// <summary>
    /// 发送次数
    /// </summary>
    public int TimesSent { get; private set; }

    /// <summary>
    /// 事件创建时间
    /// </summary>
    public DateTime CreationTime { get; private set; }

    /// <summary>
    /// 发送内容
    /// </summary>
    public string Content { get; private set; } = default!;

    /// <summary>
    /// 事务Id
    /// </summary>
    public string TransactionId { get; private set; } = default!;

    [NotMapped] public string EventTypeShortName => EventTypeName.Split('.').Last();
    [NotMapped] public Event? IntegrationEvent { get; private set; }


    public IntegrationEventLogEntry DeserializeJsonContent(Type type)
    {
        IntegrationEvent = JsonSerializer.Deserialize(Content, type)?.As<Event>();
        return this;
    }

    public void UpdateEventStatus(EventStateEnum status)
    {
        State = status;
    }

    public void TimesSentIncr(int value = 1)
    {
        TimesSent += value;
    }
}