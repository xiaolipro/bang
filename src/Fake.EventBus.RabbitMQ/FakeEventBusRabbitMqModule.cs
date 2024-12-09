using Fake.DependencyInjection;
using Fake.EventBus.Distributed;

// ReSharper disable once CheckNamespace
namespace Fake.EventBus.RabbitMQ;

[DependsOn(typeof(FakeRabbitMqModule))]
public class FakeEventBusRabbitMqModule : FakeModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.OnServiceExposing(exposingContext =>
            {
                foreach (var interfaceType in exposingContext.ImplementationType.GetInterfaces())
                {
                    if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEventHandler<>))
                    {
                        var eventType = interfaceType.GenericTypeArguments[0];
                        
                        // Use keyed services to register multiple handlers for the same event type
                        // the consumer can use IKeyedServiceProvider.GetKeyedService<IIntegrationEventHandler>(typeof(T)) to get all
                        // handlers for the event type.
                        exposingContext.ExposedServices.TryAdd(new ServiceIdentifier(eventType, typeof(IEventHandler)));

                        context.Services.Configure<EventBusSubscriptionOptions>(o =>
                            {
                                // Keep track of all registered event types and their name mapping. We send these event types over the message bus
                                // and we don't want to do Type.GetType, so we keep track of the name mapping here.

                                // This list will also be used to subscribe to events from the underlying message broker implementation.
                                o.EventTypes[eventType.Name] = eventType;
                            });
                    }
                }
            });
    }
    
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.Configure<RabbitMqEventBusOptions>(configuration);

        context.Services.AddSingleton<IEventBus, RabbitMqEventBus>();
        context.Services.AddSingleton<IDistributedEventBus, RabbitMqEventBus>();
        context.Services.AddHostedService<RabbitMqEventBus>();
    }
}