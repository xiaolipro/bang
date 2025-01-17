using Fake.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Fake.RabbitMQ.Tests;

public class RabbitMqConnectionPoolTests:ApplicationTest<FakeRabbitMqTestsModule>
{
    [Fact]
    void Should_Get_Connection()
    {
        // Arrange
        var pool = ServiceProvider.GetRequiredService<IRabbitMqConnectionPool>();

        // Action
        var connection = pool.Get();

        // Assert
        connection.ShouldNotBeNull();
    }
}