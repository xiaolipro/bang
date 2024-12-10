using Domain.Aggregates.BuyerAggregate;
using Domain.Aggregates.OrderAggregate;
using Fake.Domain.Entities;
using Fake.Domain.Repositories;
using Fake.DomainDrivenDesign.Repositories.SqlSugarCore;
using Fake.Modularity;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace Fake.SqlSugarCore.Tests;

[DependsOn(typeof(FakeAppTestModule))]
[DependsOn(typeof(FakeSqlSugarCoreModule))]
public class FakeSqlSugarCoreTestModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSugarDbContext<OrderingContext>(options =>
        {
            options.ConnectionString = "Filename=:memory:";
            options.DbType = DbType.Sqlite;
        });

        context.Services.AddTransient(typeof(IRepository<Order>),
            typeof(SqlSugarRepository<OrderingContext, Order>));
        context.Services.AddTransient(typeof(IRepository<Buyer>),
            typeof(SqlSugarRepository<OrderingContext, Buyer>));
    }

    public override void PreConfigureApplication(ApplicationConfigureContext context)
    {
        var orderingContext = context.ServiceProvider.GetRequiredService<OrderingContext>();

        var client = orderingContext.SqlSugarClient;
        //尝试创建数据库
        client.DbMaintenance.CreateDatabase();

        //如果是Order实体进行相关配置
        var types = typeof(FakeAppTestModule).Assembly.GetTypes()
            .Where(x => x.IsAssignableTo<IEntity>())
            .ToList();

        if (types.Count > 0)
        {
            client.CodeFirst.InitTables(types.ToArray());
        }
    }
}