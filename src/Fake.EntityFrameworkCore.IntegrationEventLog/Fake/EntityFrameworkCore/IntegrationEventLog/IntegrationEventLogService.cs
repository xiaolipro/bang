using System.Reflection;
using Fake.EventBus;
using Fake.EventBus.Distributed;
using Fake.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Fake.EntityFrameworkCore.IntegrationEventLog;

public class IntegrationEventLogService<TContext>(TContext context) : IIntegrationEventLogService, IDisposable
    where TContext : DbContext
{
    private static readonly List<Type> EventTypes =
        ReflectionHelper.GetAssemblyAllTypes(Assembly.GetEntryAssembly()!)
            .Where(t => t.Name.EndsWith(nameof(IntegrationEvent)))
            .ToList();

    private volatile bool _disposedValue;

    public async Task<IEnumerable<IntegrationEventLogEntry>> RetrieveEventLogsPendingToPublishAsync(Guid transactionId)
    {
        var tid = transactionId.ToString();

        var result = await context.Set<IntegrationEventLogEntry>()
            .Where(e => e.TransactionId == tid && e.State == EventStateEnum.NotPublished)
            .ToListAsync();

        if (result.Any())
        {
            return result.OrderBy(o => o.CreationTime)
                .Select(e =>
                    e.DeserializeJsonContent(EventTypes.Find(t => t.Name == e.EventTypeShortName) ??
                                             throw new FakeException($"非法的事件类型：{e.EventTypeShortName}")));
        }

        return new List<IntegrationEventLogEntry>();
    }

    public Task SaveEventAsync(IntegrationEvent @event, IDbContextTransaction? transaction = null)
    {
        //if (transaction == null) throw new ArgumentNullException(nameof(transaction));

        var eventLogEntry = new IntegrationEventLogEntry(@event, transaction?.TransactionId ?? default);

        context.Database.UseTransaction(transaction?.GetDbTransaction());
        context.IntegrationEventLogs.Add(eventLogEntry);

        return context.SaveChangesAsync();
    }

    public Task MarkEventAsPublishedAsync(Guid eventId)
    {
        return UpdateEventStatus(eventId, EventStateEnum.Published);
    }

    public Task MarkEventAsInProgressAsync(Guid eventId)
    {
        return UpdateEventStatus(eventId, EventStateEnum.InProgress);
    }

    public Task MarkEventAsFailedAsync(Guid eventId)
    {
        return UpdateEventStatus(eventId, EventStateEnum.PublishedFailed);
    }

    private Task UpdateEventStatus(Guid eventId, EventStateEnum status)
    {
        var eventLogEntry = integrationEventLogContext.IntegrationEventLogs.Single(ie => ie.EventId == eventId);
        eventLogEntry.UpdateEventStatus(status);

        if (status == EventStateEnum.InProgress)
            eventLogEntry.TimesSentIncr();

        integrationEventLogContext.IntegrationEventLogs.Update(eventLogEntry);

        return integrationEventLogContext.SaveChangesAsync();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                integrationEventLogContext.Dispose();
            }


            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}