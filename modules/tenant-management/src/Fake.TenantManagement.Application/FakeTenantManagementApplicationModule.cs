using Fake.AspNetCore;
using Fake.AspNetCore.Mvc.Conventions;
using Fake.Modularity;
using Fake.ObjectMapping.AutoMapper;
using Fake.TenantManagement.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Fake.TenantManagement.Application;

[DependsOn(typeof(FakeObjectMappingAutoMapperModule))]
[DependsOn(typeof(FakeAspNetCoreModule))]
[DependsOn(typeof(FakeTenantManagementDomainModule))]
public class FakeTenantManagementApplicationModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<FakeAutoMapperOptions>(options =>
        {
            options.ScanProfiles<FakeTenantManagementApplicationModule>(true);
        });

        context.Services.Configure<ApplicationService2ControllerOptions>(options =>
        {
            options.ScanApplicationServices<FakeTenantManagementApplicationModule>();
        });

        context.Services.AddFakeSwaggerGen();
    }

    public override void ConfigureApplication(ApplicationConfigureContext context)
    {
       
    }
}