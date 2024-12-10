using Domain.Queries;
using Fake.AppTests.Tests;
using Fake.SqlSugarCore.Tests.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Fake.SqlSugarCore.Tests.AppTests;

public class NonRootRepositoryTests : RootlessRepositoryTests<FakeSqlSugarCoreTestModule>
{
    protected override void BeforeAddFakeApplication(IServiceCollection services)
    {
        services.AddTransient<IOrderQueries, OrderQueries>();
    }
}