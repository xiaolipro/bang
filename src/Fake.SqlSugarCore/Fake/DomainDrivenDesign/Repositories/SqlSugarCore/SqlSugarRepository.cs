using System.Linq.Expressions;
using Fake.Domain.Repositories;
using Fake.SqlSugarCore;

namespace Fake.DomainDrivenDesign.Repositories.SqlSugarCore;

public class SqlSugarRepository<TDbContext, TEntity> : RepositoryBase<TEntity>, ISqlSugarRepository<TDbContext, TEntity>
    where TDbContext : SugarDbContext<TDbContext>
    where TEntity : class, IAggregateRoot, new()
{
    public async Task<TDbContext> GetDbContextAsync(CancellationToken cancellationToken = default)
    {
        return await LazyServiceProvider.GetRequiredService<ISugarDbContextProvider<TDbContext>>()
            .GetDbContextAsync(cancellationToken);
    }

    public async Task<ISimpleClient<TEntity>> GetSimpleClientAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);

        var context = await GetDbContextAsync(cancellationToken);

        return new SimpleClient<TEntity>(context.SqlSugarClient);
    }

    public async Task<ISugarQueryable<TEntity>> GetQueryableAsync(CancellationToken cancellationToken = default)
    {
        var client = await GetSimpleClientAsync(cancellationToken);
        return client.AsQueryable();
    }

    public override async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);

        var client = await GetSimpleClientAsync(cancellationToken);

        return await client.GetFirstAsync(predicate, cancellationToken);
    }

    public override async Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>>? predicate = null,
        Dictionary<string, bool>? sorting = null, CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);
        var client = await GetSimpleClientAsync(cancellationToken);
        return await client.GetListAsync(predicate, cancellationToken);
    }

    public override async Task<List<TEntity>> GetPagedListAsync(Expression<Func<TEntity, bool>>? predicate,
        int pageIndex = 1, int pageSize = 20, Dictionary<string, bool>? sorting = null,
        CancellationToken cancellationToken = default)
    {
        var client = await GetSimpleClientAsync(cancellationToken);
        return await client.GetPageListAsync(predicate, new PageModel
        {
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = 0
        }, GetCancellationToken(cancellationToken));
    }

    public override async Task<long> CountAsync(Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var client = await GetSimpleClientAsync(cancellationToken);
        return await client.CountAsync(predicate, GetCancellationToken(cancellationToken));
    }

    public override async Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var client = await GetSimpleClientAsync(cancellationToken);
        return await client.IsAnyAsync(predicate, GetCancellationToken(cancellationToken));
    }

    public override async Task<TEntity> InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var client = await GetSimpleClientAsync(cancellationToken);
        return await client.InsertReturnEntityAsync(entity, cancellationToken);
    }

    public override Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task DeleteAsync(Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}