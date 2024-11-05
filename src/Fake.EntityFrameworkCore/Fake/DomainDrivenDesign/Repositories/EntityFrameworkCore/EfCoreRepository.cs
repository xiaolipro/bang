using System.Linq.Expressions;
using Fake.Domain.Exceptions;
using Fake.Domain.Repositories;
using Fake.EntityFrameworkCore;

namespace Fake.DomainDrivenDesign.Repositories.EntityFrameWorkCore;

public class EfCoreRepository<TDbContext, TEntity> : RepositoryBase<TEntity>,
    IEfCoreRepository<TDbContext, TEntity>
    where TDbContext : EfCoreDbContext<TDbContext>
    where TEntity : class, IAggregateRoot
{
    public Task<TDbContext> GetDbContextAsync(CancellationToken cancellationToken = default)
    {
        return LazyServiceProvider.GetRequiredService<IEfDbContextProvider<TDbContext>>()
            .GetDbContextAsync(cancellationToken);
    }

    public async Task<DbSet<TEntity>> GetDbSetAsync(CancellationToken cancellationToken = default)
    {
        return (await GetDbContextAsync(cancellationToken)).Set<TEntity>();
    }

    public async Task<IQueryable<TEntity>> GetQueryableAsync(CancellationToken cancellationToken = default)
    {
        return await GetQueryableAsync(false, cancellationToken);
    }

    public virtual async Task<IQueryable<TEntity>> GetQueryableAsync(
        bool isInclude = true,
        CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);

        var dbSet = await GetDbSetAsync(cancellationToken);

        var query = dbSet.AsQueryable();
        if (isInclude)
        {
            var entityType = dbSet.EntityType.Model.FindEntityType(typeof(TEntity));

            if (entityType != null)
            {
                query = entityType.GetNavigations().Aggregate(
                    query,
                    (current, navigationProperty)
                        => current.Include(navigationProperty.Name)
                );
            }
        }

        return query;
    }

    public async Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>>? predicate = null, bool isInclude = true,
        CancellationToken cancellationToken = default)
    {
        var res = await FirstOrDefaultAsync(predicate, isInclude, cancellationToken);
        if (res == default) throw new EntityNotFoundException(typeof(TEntity));
        return res;
    }

    public override async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        return await FirstOrDefaultAsync(predicate, false, cancellationToken);
    }

    public virtual async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool isInclude = true,
        CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);

        var query = await GetQueryableAsync(isInclude, cancellationToken);

        return await query.WhereIf(predicate != null, predicate).FirstOrDefaultAsync(cancellationToken);
    }

    public override async Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>>? predicate = null,
        Dictionary<string, bool>? sorting = null,
        CancellationToken cancellationToken = default)
    {
        return await GetListAsync(predicate, sorting, false, cancellationToken);
    }

    public virtual async Task<List<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Dictionary<string, bool>? sorting = null,
        bool isInclude = true,
        CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);

        var query = await GetQueryableAsync(isInclude, cancellationToken);

        sorting ??= new Dictionary<string, bool>();
        return await query.WhereIf(predicate != null, predicate).OrderBy(sorting).ToListAsync(cancellationToken);
    }

    public override async Task<List<TEntity>> GetPaginatedListAsync(Expression<Func<TEntity, bool>>? predicate,
        int pageIndex = 1, int pageSize = 20, Dictionary<string, bool>? sorting = null,
        CancellationToken cancellationToken = default)
    {
        return await GetPaginatedListAsync(predicate, pageIndex, pageSize, sorting, false, cancellationToken);
    }

    public virtual async Task<List<TEntity>> GetPaginatedListAsync(
        Expression<Func<TEntity, bool>>? predicate,
        int pageIndex = 1,
        int pageSize = 20,
        Dictionary<string, bool>? sorting = null,
        bool isInclude = true,
        CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);

        var query = await GetQueryableAsync(isInclude, cancellationToken);

        return await query
            .WhereIf(predicate != null, predicate)
            .OrderBy(sorting)
            .PageBy(pageIndex, pageSize)
            .ToListAsync(cancellationToken);
    }

    public override async Task<long> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);

        var query = await GetQueryableAsync(false, cancellationToken);

        return await query.WhereIf(predicate != null, predicate).LongCountAsync(cancellationToken);
    }

    public override async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);

        var query = await GetQueryableAsync(false, cancellationToken);

        return await query.WhereIf(predicate != null, predicate).AnyAsync(cancellationToken);
    }

    public override async Task<TEntity> InsertAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync(cancellationToken);

        /*
         * EFCore的AddAsync与Add区别
         * https://stackoverflow.com/questions/47135262/addasync-vs-add-in-ef-core
         */
        var entry = dbContext.Add(entity);

        return entry.Entity;
    }

    public override async Task InsertRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync(cancellationToken);

        await dbContext.Set<TEntity>().AddRangeAsync(entities, cancellationToken);
    }

    public override async Task<TEntity> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync(cancellationToken);

        dbContext.Attach(entity);
        var entry = dbContext.Update(entity);

        return entry.Entity;
    }

    public override async Task UpdateRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);

        var dbContext = await GetDbContextAsync(cancellationToken);

        dbContext.Set<TEntity>().UpdateRange(entities);
    }

    public override async Task DeleteAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync(cancellationToken);

        dbContext.Set<TEntity>().Remove(entity);
    }

    public override async Task DeleteRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);

        var dbContext = await GetDbContextAsync(cancellationToken);

        dbContext.RemoveRange(entities);
    }

    public override async Task DeleteAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);
        var dbContext = await GetDbContextAsync(cancellationToken);

        var entities = await dbContext.Set<TEntity>()
            .Where(predicate)
            .ToListAsync(cancellationToken);

        await DeleteRangeAsync(entities, cancellationToken);
    }
}