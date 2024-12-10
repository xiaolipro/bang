using Fake.DependencyInjection;
using Fake.Domain.Repositories;
using Fake.SqlSugarCore;

namespace Fake.DomainDrivenDesign.Repositories.SqlSugarCore;

public class SugarRootlessRepository<TDbContext> : IRootlessRepository
    where TDbContext : SugarDbContext<TDbContext>
{
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    public ILazyServiceProvider LazyServiceProvider { get; set; } = default!; // 属性注入

    private ISugarDbContextProvider<TDbContext> SugarDbContextProvider =>
        LazyServiceProvider.GetRequiredService<ISugarDbContextProvider<TDbContext>>();

    public async Task<ISqlSugarClient> GetDbContextAsync(CancellationToken cancellationToken = default)
    {
        var context = await SugarDbContextProvider.GetDbContextAsync(cancellationToken);
        return context.SqlSugarClient;
    }
}