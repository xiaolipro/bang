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

        context.Services.AddSingleton<IRabbitMqConnectionProvider, RabbitMqConnectionProvider>();
        context.Services.AddSingleton<IRabbitMqChannelPool, RabbitMqChannelPool>();
    }
}