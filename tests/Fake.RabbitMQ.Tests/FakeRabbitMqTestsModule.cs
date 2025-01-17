using Fake.Autofac;
using Fake.Modularity;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace Fake.RabbitMQ.Tests;

[DependsOn(typeof(FakeAutofacModule))]
[DependsOn(typeof(FakeRabbitMqModule))]
public class FakeRabbitMqTestsModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<FakeRabbitMqOptions>(options =>
            {
                options.Connections = new Dictionary<string, ConnectionFactory>
                {
                    ["Default"] = new()
                    {
                        HostName = "localhost",
                        Port = 5672,
                        UserName = "rabbit",
                        Password = "itd!@#123"
                    }
                };
            });
    }
}