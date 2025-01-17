using Fake.Modularity;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Fake.RabbitMQ;

public class FakeRabbitMqModule: FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.Configure<FakeRabbitMqOptions>(configuration.GetSection("RabbitMQ"));
        
        // important：async consumer
        context.Services.Configure<FakeRabbitMqOptions>(options =>
            {
                foreach (var connectionFactory in options.Connections.Values)
                {
                    connectionFactory.DispatchConsumersAsync = true;
                }
            });

        context.Services.AddSingleton<IRabbitMqConnectionPool, RabbitMqConnectionPool>();
        context.Services.AddSingleton<IRabbitMqChannelPool, RabbitMqChannelPool>();
    }
}