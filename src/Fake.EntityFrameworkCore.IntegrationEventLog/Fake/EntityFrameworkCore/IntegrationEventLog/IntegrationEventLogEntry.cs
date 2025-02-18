using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Fake.EventBus;
using Fake.EventBus.Distributed;
// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace Fake.EntityFrameworkCore.IntegrationEventLog;

public class IntegrationEventLogEntry
{
    private static readonly JsonSerializerOptions s_indentedOptions = new() { WriteIndented = true };
    private static readonly JsonSerializerOptions s_caseInsensitiveOptions = new() { PropertyNameCaseInsensitive = true };
    
    public IntegrationEventLogEntry(IntegrationEvent @event, Guid transactionId)
    {
        EventId = @event.Id;
        CreationTime = @event.CreationTime;
        EventName = @event.GetType().Name;
        Content = JsonSerializer.Serialize(@event, s_indentedOptions);
        State = EventStateEnum.NotPublished;
        TimesSent = 0;
        TransactionId = transactionId.ToString();
    }

    public Guid EventId { get; private set; }
    
    /// <summary>
    /// 事件名称
    /// </summary>
    public string EventName { get; private set; }

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
    public string Content { get; private set; }

    /// <summary>
    /// 事务Id
    /// </summary>
    public string TransactionId { get; private set; }

    [NotMapped] public string EventTypeShortName => EventName.Split('.').Last();
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