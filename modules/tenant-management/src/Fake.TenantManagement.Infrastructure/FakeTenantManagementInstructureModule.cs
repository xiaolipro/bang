using Fake.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace Fake.TenantManagement.Infrastructure;

public class FakeTenantManagementInfrastructureModule: FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddDbContextFactory<TenantManagementDbContext>(options =>
        {
        });
    }

    public override void ConfigureApplication(ApplicationConfigureContext context)
    {
    }
}