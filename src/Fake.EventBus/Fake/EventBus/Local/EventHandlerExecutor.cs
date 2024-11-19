using System;
using System.Threading.Tasks;

namespace Fake.EventBus.Local;

public record EventHandlerExecutor(object HandlerInstance, Func<Event, Task> HandleFunc)
{
    public object HandlerInstance { get; } = HandlerInstance;
    public Func<Event, Task> HandleFunc { get; } = HandleFunc;
}