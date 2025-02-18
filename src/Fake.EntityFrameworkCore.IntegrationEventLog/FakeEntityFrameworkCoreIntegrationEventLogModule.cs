using Fake.Modularity;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Fake.EntityFrameworkCore.IntegrationEventLog;

[DependsOn(typeof(FakeEntityFrameworkCoreModule))]
public class FakeEntityFrameworkCoreIntegrationEventLogModule:FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        //context.Services.AddTransient<IIntegrationEventLogService, IntegrationEventLogService>();
    }
}