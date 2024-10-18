using Fake.Autofac;
using Fake.Helpers;
using Fake.Modularity;
using Fake.SyncEx;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Fake.EntityFrameworkCore.IntegrationEventLog.Tests;

[DependsOn(typeof(FakeAutofacModule))]
[DependsOn(typeof(FakeEntityFrameworkCoreIntegrationEventLogModule))]
public class FakeEntityFrameworkCoreIntegrationEventLogTestModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddDbContextFactory<IntegrationEventLogContext>(builder =>
        {
            //使用sqlite内存模式要开open
            var options = new SqliteConnection("Filename=:memory:");
            options.Open();
            builder.UseSqlite(options).UseLoggerFactory(LoggerFactory.Create(loggingBuilder =>
            {
                loggingBuilder
                    .AddFilter((category, level) =>
                        category == DbLoggerCategory.Database.Command.Name && level == LogLevel.Information) // 仅记录命令信息
                    .AddConsole(); // 输出到控制台
            }));
#if DEBUG
            builder.EnableSensitiveDataLogging();
#endif
            /*builder.UseSqlite("FileName=./fake.db");*/
        });

        context.Services.Replace(new ServiceDescriptor(typeof(IntegrationEventLogContext),
            typeof(IntegrationEventLogContext),
            ServiceLifetime.Transient));
    }

    public override void PreConfigureApplication(ApplicationConfigureContext context)
    {
        var ctx = context.ServiceProvider.GetRequiredService<IntegrationEventLogContext>();

        using (ctx)
        {
            SyncContext.Run(async () =>
            {
                await ctx.Database.EnsureCreatedAsync();
                await ctx.Database.MigrateAsync();
            });
        }
    }
}