using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fake.EventBus;
using Microsoft.EntityFrameworkCore.Storage;

namespace Fake.EntityFrameworkCore.IntegrationEventLog;

public interface IIntegrationEventLogService : IDisposable
{
    Task<IEnumerable<IntegrationEventLogEntry>> RetrieveEventLogsPendingToPublishAsync(Guid transactionId);

    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="transaction" /> is <see langword="null" />.
    /// </exception>
    Task SaveEventAsync(Event integrationEvent, IDbContextTransaction? transaction = null);

    Task MarkEventAsPublishedAsync(Guid eventId);
    Task MarkEventAsInProgressAsync(Guid eventId);
    Task MarkEventAsFailedAsync(Guid eventId);
}