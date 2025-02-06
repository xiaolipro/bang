using System.Reflection;
using System.Text;
using Fake.Data.Filtering;
using Fake.DependencyInjection;
using Fake.Domain.Entities.Auditing;
using Fake.Domain.Events;
using Fake.EventBus.Local;
using Fake.Helpers;
using Fake.IdGenerators;
using Fake.SyncEx;
using Fake.Timing;

namespace Fake.SqlSugarCore;

public abstract class SugarDbContext<TDbContext> where TDbContext : SugarDbContext<TDbContext>
{
    public ILazyServiceProvider ServiceProvider { get; set; } = null!;
    public ISqlSugarClient SqlSugarClient { get; private set; }

    protected readonly SugarDbConnOptions<TDbContext> Options;
    protected IFakeClock FakeClock => ServiceProvider.GetRequiredService<IFakeClock>();
    protected GuidGeneratorBase GuidGenerator => ServiceProvider.GetRequiredService<GuidGeneratorBase>();
    protected LongIdGeneratorBase LongIdGenerator => ServiceProvider.GetRequiredService<LongIdGeneratorBase>();
    protected ILocalEventBus LocalEventBus => ServiceProvider.GetRequiredService<ILocalEventBus>();
    protected IAuditPropertySetter AuditPropertySetter => ServiceProvider.GetRequiredService<IAuditPropertySetter>();
    protected IDataFilter DataFilter => ServiceProvider.GetRequiredService<IDataFilter>();

    protected ILogger<SugarDbContext<TDbContext>> Logger =>
        ServiceProvider.GetRequiredService<ILogger<SugarDbContext<TDbContext>>>();

    public SugarDbContext(SugarDbConnOptions<TDbContext> options)
    {
        Options = options;
        var config = new ConnectionConfig
        {
            ConfigId = Options.ConfigId,
            DbType = Options.DbType,
            ConnectionString = Options.ConnectionString,
            SlaveConnectionConfigs = Options.ReadConnectionStrings
                .Select(str => new SlaveConnectionConfig { ConnectionString = str }).ToList(),
            IsAutoCloseConnection = Options.IsAutoCloseConnection,
            AopEvents = new AopEvents
            {
                OnDiffLogEvent = null,
                OnError = null,
                OnLogExecuting = OnLogExecuting,
                OnLogExecuted = OnLogExecuted,
                OnExecutingChangeSql = null,
                DataExecuting = DataExecuting,
                DataExecuted = DataExecuted
            },
            ConfigureExternalServices = new ConfigureExternalServices
            {
                EntityService = ConfigureEntityService
            },
            IndexSuffix = null,
            SqlMiddle = null
        };

        ConfigureConnection(config);

        SqlSugarClient = new SqlSugarClient(config);

        SqlSugarClient.Ado.CommandTimeOut = options.Timeout;
    }

    public virtual void Initialize(int timeout)
    {
        // 设置超时时间
        SqlSugarClient.Ado.CommandTimeOut = timeout;
        ConfigureGlobalFilters();
    }

    protected virtual void ConfigureGlobalFilters()
    {
        //需自定义扩展
        if (DataFilter.IsEnabled<ISoftDelete>())
        {
            SqlSugarClient.QueryFilter.AddTableFilter<ISoftDelete>(u => u.IsDeleted == false);
        }
    }

    protected virtual void ConfigureConnection(ConnectionConfig action)
    {
    }

    protected virtual void ConfigureEntityService(PropertyInfo property, EntityColumnInfo column)
    {
        if (property.Name == nameof(Entity<AnyKey>.Id))
        {
            column.IsPrimarykey = true;
        }

        if (new NullabilityInfoContext().Create(property).WriteState is NullabilityState.Nullable)
        {
            column.IsNullable = true;
        }

        if (property.Name == nameof(IHasDomainEvent.DomainEvents))
        {
            column.IsIgnore = true;
        }

        if (property.Name == nameof(IHasExtraProperties.ExtraProperties))
        {
            column.IsIgnore = true;
        }
    }

    protected virtual async Task PublishDomainEventsAsync(IHasDomainEvent entityWithDomainEvent)
    {
        if (entityWithDomainEvent.DomainEvents == null) return;
        foreach (var @event in entityWithDomainEvent.DomainEvents)
        {
            await LocalEventBus.PublishAsync(@event);
        }
    }

    #region DataExecuting

    protected virtual void DataExecuting(object oldValue, DataFilterModel entityInfo)
    {
        if (entityInfo.EntityValue is not IEntity entity) return;

        switch (entityInfo.OperationType)
        {
            case DataFilterType.UpdateByObject:
                AuditPropertySetter.SetModificationProperties(entity);
                break;
            case DataFilterType.InsertByObject:
                AuditPropertySetter.SetCreationProperties(entity);
                break;
            case DataFilterType.DeleteByObject:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected virtual void DataExecuted(object oldValue, DataAfterModel entityInfo)
    {
        if (oldValue is IHasDomainEvent entity)
        {
            SyncContext.Run(() => PublishDomainEventsAsync(entity));
        }
    }

    #endregion

    #region 日志

    protected virtual void OnLogExecuting(string sql, SugarParameter[] pars)
    {
        if (Options.EnabledSqlLog)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("SQL SUGAR LOG:");
            sb.AppendLine(UtilMethods.GetSqlString(DbType.SqlServer, sql, pars));
            Logger.LogDebug(sb.ToString());
        }
    }

    protected virtual void OnLogExecuted(string sql, SugarParameter[] pars)
    {
        if (Options.EnabledSqlLog)
        {
            Logger.LogDebug("- ExecutionDuration: {times} ms", SqlSugarClient.Ado.SqlExecutionTime.TotalMilliseconds);
        }
    }

    #endregion
}