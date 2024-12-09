using Fake.DependencyInjection;
using Fake.EventBus.Local;
using Fake.Modularity;
using Fake.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Fake.EventBus;

[DependsOn(typeof(FakeUnitOfWorkModule))]
public class FakeEventBusModule : FakeModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.OnServiceExposing(exposingContext =>
            {
                foreach (var interfaceType in exposingContext.ImplementationType.GetInterfaces())
                {
                    if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEventHandler<>))
                    {
                        exposingContext.ExposedServices.TryAdd(new ServiceIdentifier(interfaceType));
                    }
                }
            });
    }
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IEventBus, LocalEventBus>();
        context.Services.AddSingleton<ILocalEventBus, LocalEventBus>();
    }
}