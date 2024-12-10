using Fake.Domain.Repositories;
using Fake.SqlSugarCore;

namespace Fake.DomainDrivenDesign.Repositories.SqlSugarCore;

public interface ISqlSugarRepository<TDbContext, TEntity> : IRepository<TEntity>
    where TDbContext : SugarDbContext<TDbContext>
    where TEntity : class, IAggregateRoot, new()
{
    Task<TDbContext> GetDbContextAsync(CancellationToken cancellationToken = default);

    Task<ISimpleClient<TEntity>> GetSimpleClientAsync(CancellationToken cancellationToken = default);

    Task<ISugarQueryable<TEntity>> GetQueryableAsync(CancellationToken cancellationToken = default);
}