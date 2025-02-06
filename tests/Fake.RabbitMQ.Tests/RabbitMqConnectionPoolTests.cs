using Fake.Testing;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client.Exceptions;
using Shouldly;
using Xunit;

namespace Fake.RabbitMQ.Tests;

public class RabbitMqConnectionPoolTests : ApplicationTest<FakeRabbitMqTestsModule>
{
    [Fact]
    void Should_Get_Connection()
    {
        // Arrange
        var pool = ServiceProvider.GetRequiredService<IRabbitMqConnectionPool>();

        // Action
        Assert.Throws<BrokerUnreachableException>(() =>
            {
                var connection = pool.Get();
                connection.ShouldNotBeNull();
            });
    }
}