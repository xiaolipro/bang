using Microsoft.Extensions.DependencyInjection;

namespace Fake.EventBus;

public interface IEventBusBuilder
{
    public IServiceCollection Services { get; }
}