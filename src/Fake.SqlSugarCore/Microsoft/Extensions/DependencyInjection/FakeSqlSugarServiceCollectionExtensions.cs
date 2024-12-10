using Fake.SqlSugarCore;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class FakeSqlSugarServiceCollectionExtensions
{
    public static IServiceCollection AddSugarDbContext<TDbContext>(this IServiceCollection services,
        Action<SugarDbConnOptions<TDbContext>>? optionsAction = null,
        ServiceLifetime lifetime = ServiceLifetime.Transient) where TDbContext : SugarDbContext<TDbContext>
    {
        var configuration = services.GetConfiguration();
        var options = configuration.GetSection(typeof(TDbContext).Name).Get<SugarDbConnOptions<TDbContext>>() ?? new();
        optionsAction?.Invoke(options);

        services.TryAdd(new ServiceDescriptor(
            typeof(SugarDbConnOptions<TDbContext>),
            sp => options,
            ServiceLifetime.Singleton));

        services.TryAdd(
            new ServiceDescriptor(
                typeof(TDbContext),
                typeof(TDbContext),
                lifetime == ServiceLifetime.Transient
                    ? ServiceLifetime.Transient
                    : ServiceLifetime.Scoped));

        return services;
    }
}