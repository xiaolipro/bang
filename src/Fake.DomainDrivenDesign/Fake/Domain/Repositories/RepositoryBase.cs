using System.Linq.Expressions;
using Fake.DependencyInjection;
using Fake.Domain.Entities;
using Fake.Domain.Exceptions;
using Fake.Threading;

namespace Fake.Domain.Repositories;

/// <summary>
/// 仓储基类
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public abstract class RepositoryBase<TEntity> : IRepository<TEntity>
    where TEntity : class, IAggregateRoot
{
    public ILazyServiceProvider LazyServiceProvider { get; set; } = null!; // 属性注入

    public ICancellationTokenProvider CancellationTokenProvider =>
        LazyServiceProvider.GetRequiredService<ICancellationTokenProvider>();

    public IUnitOfWorkManager UnitOfWorkManager => LazyServiceProvider.GetRequiredService<IUnitOfWorkManager>();
    public IUnitOfWork? UnitOfWork => UnitOfWorkManager.Current;

    protected virtual CancellationToken GetCancellationToken(CancellationToken preferredValue = default)
    {
        return CancellationTokenProvider.FallbackToProvider(preferredValue);
    }

    protected virtual Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return UnitOfWork != null
            ? UnitOfWork.SaveChangesAsync(cancellationToken)
            : Task.CompletedTask;
    }

    public async Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var res = await FirstOrDefaultAsync(predicate, cancellationToken);
        if (res == default) throw new EntityNotFoundException(typeof(TEntity));
        return res;
    }

    public abstract Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    public abstract Task<List<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Dictionary<string, bool>? sorting = null,
        CancellationToken cancellationToken = default);

    public abstract Task<List<TEntity>> GetPagedListAsync(
        Expression<Func<TEntity, bool>>? predicate,
        int pageIndex = 1,
        int pageSize = 20,
        Dictionary<string, bool>? sorting = null,
        CancellationToken cancellationToken = default);

    public abstract Task<long> CountAsync(Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    public abstract Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    public abstract Task<TEntity> InsertAsync(TEntity entity, CancellationToken cancellationToken = default);

    public virtual async Task InsertRangeAsync(IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
        {
            await InsertAsync(entity, cancellationToken: cancellationToken);
        }
    }

    public abstract Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    public virtual async Task UpdateRangeAsync(IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
        {
            await UpdateAsync(entity, cancellationToken: cancellationToken);
        }
    }

    public abstract Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

    public virtual async Task DeleteRangeAsync(IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
        {
            await DeleteAsync(entity, cancellationToken: cancellationToken);
        }
    }

    public abstract Task DeleteAsync(Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);
}