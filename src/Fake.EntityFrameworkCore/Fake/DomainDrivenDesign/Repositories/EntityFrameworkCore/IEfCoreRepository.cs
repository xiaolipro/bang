using System.Linq.Expressions;
using Fake.Domain.Repositories;

namespace Fake.DomainDrivenDesign.Repositories.EntityFrameWorkCore;

public interface IEfCoreRepository<TDbContext, TEntity> : IRepository<TEntity>
    where TEntity : class, IAggregateRoot
{
    Task<TDbContext> GetDbContextAsync(CancellationToken cancellationToken = default);

    Task<DbSet<TEntity>> GetDbSetAsync(CancellationToken cancellationToken = default);

    public Task<IQueryable<TEntity>> GetQueryableAsync(CancellationToken cancellationToken = default);

    public Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>>? predicate = null, bool isInclude = true,
        CancellationToken cancellationToken = default);

    public Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool isInclude = true,
        CancellationToken cancellationToken = default);

    public Task<List<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Dictionary<string, bool>? sorting = null,
        bool isInclude = true,
        CancellationToken cancellationToken = default);

    public Task<List<TEntity>> GetPagedListAsync(
        Expression<Func<TEntity, bool>>? predicate,
        int pageIndex = 1,
        int pageSize = 20,
        Dictionary<string, bool>? sorting = null,
        bool isInclude = true,
        CancellationToken cancellationToken = default);
}