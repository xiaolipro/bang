using Fake.SqlSugarCore;

namespace Fake.UnitOfWork.SqlSugarCore;

/// <summary>
/// SqlSugarCore事务api
/// </summary>
public class SqlSugarTransactionApi<TDbContext> : ITransactionApi, ISupportRollback
    where TDbContext : SugarDbContext<TDbContext>
{
    private readonly SugarDbContext<TDbContext> _sugarDbContext;


    public SqlSugarTransactionApi(SugarDbContext<TDbContext> sugarDbContext)
    {
        _sugarDbContext = sugarDbContext;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await _sugarDbContext.SqlSugarClient.Ado.CommitTranAsync();
    }

    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        await _sugarDbContext.SqlSugarClient.Ado.RollbackTranAsync();
    }

    public SugarDbContext<TDbContext> GetDbContext()
    {
        return _sugarDbContext;
    }

    public void Dispose()
    {
        _sugarDbContext.SqlSugarClient.Ado.Dispose();
    }
}