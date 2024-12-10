using System.Diagnostics;
using Fake.SqlSugarCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fake.UnitOfWork.SqlSugarCore;

/// <summary>
/// 基于工作单元的DbContext
/// </summary>
/// <typeparam name="TDbContext"></typeparam>
public class UowSugarDbContextProvider<TDbContext>(
    IUnitOfWorkManager unitOfWorkManager,
    ICancellationTokenProvider cancellationTokenProvider,
    IConnectionStringResolver connectionStringResolver)
    : ISugarDbContextProvider<TDbContext>
    where TDbContext : SugarDbContext<TDbContext>
{
    public readonly ILogger<UowSugarDbContextProvider<TDbContext>> Logger =
        NullLogger<UowSugarDbContextProvider<TDbContext>>
            .Instance;

    public async Task<TDbContext> GetDbContextAsync(CancellationToken cancellationToken = default)
    {
        var unitOfWork = unitOfWorkManager.Current;

        if (unitOfWork == null)
        {
            throw new FakeException($"{typeof(TDbContext).Name}必须在工作单元内创建！");
        }

        var targetDbContextType = typeof(TDbContext);
        var connectionStringName = ConnectionStringNameAttribute.GetConnStringName<TDbContext>();
        var connectionString = await connectionStringResolver.ResolveAsync(connectionStringName);

        var dbContextKey = $"{targetDbContextType.FullName}_{connectionString}";
        var databaseApi = unitOfWork.FindDatabaseApi(dbContextKey);

        if (databaseApi == null)
        {
            var dbContext = await CreateDbContextAsync(unitOfWork, connectionString, cancellationToken);
            databaseApi = new SqlSugarDatabaseApi<TDbContext>(dbContext);

            unitOfWork.AddDatabaseApi(dbContextKey, databaseApi);
        }

        return ((SqlSugarDatabaseApi<TDbContext>)databaseApi).DbContext;
    }


    private async Task<TDbContext> CreateDbContextAsync(IUnitOfWork unitOfWork,
        string connectionString, CancellationToken cancellationToken = default)
    {
        var creationContext = new DbContextCreationContext(connectionString);
        using (DbContextCreationContext.Use(creationContext))
        {
            var dbContext = unitOfWork.Context.IsTransactional
                ? await CreateDbContextWithTransactionAsync(unitOfWork, cancellationToken)
                : unitOfWork.ServiceProvider.GetRequiredService<TDbContext>();

            dbContext.Initialize(unitOfWork.Context.Timeout);
            return dbContext;
        }
    }


    private async Task<TDbContext> CreateDbContextWithTransactionAsync(IUnitOfWork unitOfWork,
        // ReSharper disable once UnusedParameter.Local
        CancellationToken cancellationToken = default)
    {
        Debug.Assert(DbContextCreationContext.Current != null, "DbContextCreationContext.Current != null");

        //事务key
        var transactionApiKey = $"SqlSugarCore_{DbContextCreationContext.Current.ConnectionString}";

        //尝试查找事务
        var activeTransaction = unitOfWork.FindTransactionApi(transactionApiKey) as SqlSugarTransactionApi<TDbContext>;

        //该db还没有进行开启事务
        if (activeTransaction == null)
        {
            //获取到db添加事务即可
            var dbContext = unitOfWork.ServiceProvider.GetRequiredService<TDbContext>();
            var transaction = new SqlSugarTransactionApi<TDbContext>(
                dbContext
            );
            unitOfWork.AddTransactionApi(transactionApiKey, transaction);

            await dbContext.SqlSugarClient.Ado.BeginTranAsync();
            return dbContext;
        }

        return (TDbContext)activeTransaction.GetDbContext();
    }

    protected virtual CancellationToken GetCancellationToken(CancellationToken preferredValue = default)
    {
        return cancellationTokenProvider.FallbackToProvider(preferredValue);
    }
}