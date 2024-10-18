using Domain.Aggregates.BuyerAggregate;
using Domain.Aggregates.OrderAggregate;
using Fake.Domain.Repositories;
using Fake.DomainDrivenDesign.Repositories.EntityFrameWorkCore;
using Fake.EntityFrameworkCore.Tests.Repositories;
using Fake.Helpers;
using Fake.Modularity;
using Fake.SyncEx;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Fake.EntityFrameworkCore.Tests;

[DependsOn(typeof(FakeAppTestModule))]
[DependsOn(typeof(FakeEntityFrameworkCoreModule))]
public class FakeEntityFrameworkCoreTestModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient(typeof(IRepository<Order>),
            typeof(EfCoreRepository<OrderingContext, Order>));
        context.Services.AddTransient(typeof(IRepository<Buyer>),
            typeof(EfCoreRepository<OrderingContext, Buyer>));
        context.Services.AddTransient(typeof(IEfCoreRepository<OrderingContext, Order>),
            typeof(EfCoreRepository<OrderingContext, Order>));
        context.Services.AddTransient(typeof(IEfCoreRepository<OrderingContext, Buyer>),
            typeof(EfCoreRepository<OrderingContext, Buyer>));
        context.Services.AddTransient(typeof(IOrderRepository),
            typeof(OrderRepository));
        context.Services.AddTransient(typeof(IBuyerRepository),
            typeof(BuyerRepository));

        context.Services.AddDbContextFactory<OrderingContext>(builder =>
        {
            //使用sqlite内存模式要open
            var connection = new SqliteConnection("Filename=:memory:");
            connection.Open();
            builder.UseSqlite(connection);
#if DEBUG
            builder.EnableSensitiveDataLogging();
            // var sp = context.Services.GetObjectAccessorOrNull<IServiceProvider>();
            // builder.AddInterceptors(sp.Value!.GetRequiredService<FakeDbCommandInterceptor>());
#endif
        });

        // context.Services.Replace(new ServiceDescriptor(typeof(OrderingContext), typeof(OrderingContext),
        //     ServiceLifetime.Transient));
    }

    public override void PreConfigureApplication(ApplicationConfigureContext context)
    {
        var orderingContext = context.ServiceProvider.GetRequiredService<OrderingContext>();

        using (orderingContext)
        {
            SyncContext.Run(() => SeedAsync(orderingContext));
        }
    }


    private async Task SeedAsync(OrderingContext context)
    {
        await context.Database.EnsureCreatedAsync();
        await context.Database.MigrateAsync();

        await context.SaveChangesAsync();
    }
}