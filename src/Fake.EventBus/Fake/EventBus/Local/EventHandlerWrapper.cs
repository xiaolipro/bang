using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Fake.EventBus.Local;

public abstract class EventHandlerWrapper
{
    public abstract Task HandleAsync(Event @event, IServiceProvider serviceProvider,
        Func<IEnumerable<EventHandlerExecutor>, Event, Task> publish);
}

public class EventHandlerWrapperImpl<TEvent> : EventHandlerWrapper where TEvent : Event
{
    public override Task HandleAsync(Event @event, IServiceProvider serviceProvider,
        Func<IEnumerable<EventHandlerExecutor>, Event, Task> publish)
    {
        var handlers = serviceProvider
            .GetServices<EventHandler<TEvent>>()
            .Select(handler => new EventHandlerExecutor(handler,
                theEvent => handler.HandleAsync((TEvent)theEvent)));

        return publish(handlers, @event);
    }
}