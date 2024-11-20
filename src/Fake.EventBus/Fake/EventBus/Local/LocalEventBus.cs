using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fake.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fake.EventBus.Local;

public class LocalEventBus(
    ILogger<LocalEventBus> logger,
    IServiceScopeFactory serviceScopeFactory)
    : ILocalEventBus
{
    private readonly ConcurrentDictionary<Type, EventHandlerWrapper> _eventHandlers = new();

    public virtual async Task PublishAsync(Event @event)
    {
        ThrowHelper.ThrowIfNull(@event, nameof(@event));

        var eventHandler = _eventHandlers.GetOrAdd(@event.GetType(), eventType =>
        {
            var wrapper = ReflectionHelper.CreateInstance(typeof(EventHandlerWrapperImpl<>).MakeGenericType(eventType))
                .To<EventHandlerWrapper>();

            if (wrapper == null)
            {
                throw new FakeException("Cannot create event handler wrapper.");
            }

            return wrapper;
        });

        using var scope = serviceScopeFactory.CreateScope();
        await eventHandler.HandleAsync(@event, scope.ServiceProvider, ProcessingEventAsync);
    }

    protected virtual async Task ProcessingEventAsync(IEnumerable<EventHandlerExecutor> eventHandlerExecutors,
        Event @event)
    {
        // 广播事件
        foreach (var eventHandlerExecutor in eventHandlerExecutors)
        {
            logger.LogDebug("Processing event {EventName} with handler {EventHandlerName}",
                @event.GetType().Name, eventHandlerExecutor.GetType().Name);
            await eventHandlerExecutor.HandleFunc(@event);
        }
    }
}