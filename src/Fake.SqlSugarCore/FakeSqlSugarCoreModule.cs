using Fake.DomainDrivenDesign;
using Fake.Modularity;
using Fake.UnitOfWork.SqlSugarCore;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Fake.SqlSugarCore;

[DependsOn(typeof(FakeDomainDrivenDesignModule))]
public class FakeSqlSugarCoreModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient(typeof(ISugarDbContextProvider<>), typeof(UowSugarDbContextProvider<>));
    }
}