using Application.DomainEventHandlers.BuyerAndPaymentMethodVerifiedEvent;
using Application.DomainEventHandlers.OrderStartedEvent;
using Domain.Events;
using Fake.Autofac;
using Fake.DomainDrivenDesign;
using Fake.EventBus;
using Fake.Helpers;
using Fake.Modularity;
using Fake.SyncEx;

[DependsOn(typeof(FakeAutofacModule))]
[DependsOn(typeof(FakeDomainDrivenDesignModule))]
public class FakeAppTestModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton(typeof(IEventHandler),
            typeof(ValidateOrAddBuyerAggregateWhenOrderStartedDomainEventHandler));

        context.Services.AddSingleton(typeof(IEventHandler),
            typeof(UpdateOrderWhenBuyerAndPaymentMethodVerifiedDomainEventHandler));

        context.Services.AddTransient<AppTestDataBuilder>();
    }

    public override void ConfigureApplication(ApplicationConfigureContext context)
    {
        SyncContext.Run(() => context.ServiceProvider.GetRequiredService<AppTestDataBuilder>().BuildAsync());
    }
}