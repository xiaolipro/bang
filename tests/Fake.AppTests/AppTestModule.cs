using Fake.Autofac;
using Fake.DomainDrivenDesign;
using Fake.Modularity;
using Fake.SyncEx;

[DependsOn(typeof(FakeAutofacModule))]
[DependsOn(typeof(FakeDomainDrivenDesignModule))]
public class FakeAppTestModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<AppTestDataBuilder>();
    }

    public override void ConfigureApplication(ApplicationConfigureContext context)
    {
        SyncContext.Run(() => context.ServiceProvider.GetRequiredService<AppTestDataBuilder>().BuildAsync());
    }
}