using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Fake.Data.Filtering;
using Fake.DependencyInjection;
using Fake.Domain;
using Fake.Domain.Events;
using Fake.EntityFrameworkCore.Auditing;
using Fake.EntityFrameworkCore.Modeling;
using Fake.EntityFrameworkCore.ValueConverters;
using Fake.EventBus.Local;
using Fake.Helpers;
using Fake.IdGenerators;
using Fake.IdGenerators.GuidGenerator;
using Fake.UnitOfWork;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;

namespace Fake.EntityFrameworkCore;

public abstract class EfCoreDbContext<TDbContext>(DbContextOptions<TDbContext> options) : DbContext(options)
    where TDbContext : DbContext
{
    public ILazyServiceProvider ServiceProvider { get; set; } = null!;

    private static readonly MethodInfo ConfigureBasePropertiesMethodInfo = typeof(EfCoreDbContext<TDbContext>)
        .GetMethod(
            nameof(ConfigureBaseProperties),
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;

    protected IFakeClock FakeClock => ServiceProvider.GetRequiredService<IFakeClock>();
    protected ILocalEventBus LocalEventBus => ServiceProvider.GetRequiredService<ILocalEventBus>();
    protected IAuditPropertySetter AuditPropertySetter => ServiceProvider.GetRequiredService<IAuditPropertySetter>();
    protected IEntityChangeHelper EntityChangeHelper => ServiceProvider.GetRequiredService<IEntityChangeHelper>();
    protected IDataFilter DataFilter => ServiceProvider.GetRequiredService<IDataFilter>();

    public ILogger<EfCoreDbContext<TDbContext>> Logger =>
        ServiceProvider.GetRequiredService<ILogger<EfCoreDbContext<TDbContext>>>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        TrySetDatabaseProvider(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            ConfigureBasePropertiesMethodInfo
                .MakeGenericMethod(entityType.ClrType)
                .Invoke(this, [modelBuilder, entityType]);

            ConfigureValueConverter(modelBuilder, entityType);
        }

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var changes = EntityChangeHelper.CreateChangeList(ChangeTracker.Entries().ToList());

            await BeforeSaveChangesAsync();

            var res = await base.SaveChangesAsync(cancellationToken);
            await PublishDomainEventsAsync(cancellationToken);

            if (changes != null)
            {
                // id, time ..
                EntityChangeHelper.UpdateChangeList(changes);
            }

            return res;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (ex.Entries.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine(ex.Entries.Count > 1
                    ? "There are some entries which are not saved due to concurrency exception:"
                    : "There is an entry which is not saved due to concurrency exception:");
                foreach (var entry in ex.Entries)
                {
                    sb.AppendLine(entry.ToString());
                }

                Logger.LogWarning(sb.ToString());
            }

            throw new FakeDbConcurrencyException(ex);
        }
        finally
        {
            ChangeTracker.AutoDetectChangesEnabled = true;
        }
    }

    protected virtual async Task PublishDomainEventsAsync(CancellationToken cancellationToken)
    {
        var domainEvents = new List<DomainEvent>();

        // entries for more infomation:
        // https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.changetracking.changetracker.entries?view=efcore-8.0&viewFallbackFrom=net-8.0
        foreach (var entry in base.ChangeTracker.Entries<Entity>().ToList())
        {
            if (entry.Entity is IHasDomainEvent { DomainEvents: not null } hasDomainEvent &&
                hasDomainEvent.DomainEvents.Count != 0)
            {
                domainEvents.AddRange(hasDomainEvent.DomainEvents);
                hasDomainEvent.ClearDomainEvents();
            }
        }

        if (!domainEvents.Any()) return;

        await domainEvents.ForEachAsync(@event => LocalEventBus.PublishAsync(@event));

        await SaveChangesAsync(cancellationToken);
    }

    protected virtual Task BeforeSaveChangesAsync()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (!entry.State.IsIn(EntityState.Modified, EntityState.Deleted)) continue;

            if (entry.Entity is IAggregateRoot aggregate)
            {
                Entry(aggregate).Property(x => x.ConcurrencyStamp).OriginalValue = aggregate.ConcurrencyStamp;

                aggregate.ConcurrencyStamp = SimpleGuidGenerator.Instance.Generate();
            }
        }

        return Task.CompletedTask;
    }

    public virtual void Initialize(IUnitOfWork unitOfWork)
    {
        // 设置超时时间
        if (Database.IsRelational())
        {
            if (!Database.GetCommandTimeout().HasValue)
            {
                Database.SetCommandTimeout(TimeSpan.FromSeconds(unitOfWork.Context.Timeout));
            }
        }

        // 级联删除策略
        ChangeTracker.CascadeDeleteTiming = CascadeTiming.OnSaveChanges;

        // 实体Track事件
        ChangeTracker.Tracked += (_, args) =>
        {
            // TODO：ExtraProperties Tracked?

            // 为跟踪实体发布事件
            PublishEventsForTrackedEntity(args.Entry);
        };

        // 实体状态变更事件
        ChangeTracker.StateChanged += (_, args) =>
        {
            // 为跟踪实体发布事件
            PublishEventsForTrackedEntity(args.Entry);
        };
    }

    protected virtual void PublishEventsForTrackedEntity(EntityEntry entry)
    {
        if (entry.Entity is not IEntity entity) return;

        switch (entry.State)
        {
            case EntityState.Detached: //游离状态：上下文未跟踪该实体
                break;
            case EntityState.Unchanged: //未变更状态：该实体正在由上下文跟踪并存在于数据库中，其属性值与数据库中的值没有变化。
                break;
            case EntityState.Deleted: //删除状态：该实体正在由上下文跟踪并存在于数据库中，它已标记为从数据库中删除
                if (entity is ISoftDelete)
                {
                    // tips: 这里必須重置entity状态，设置完软删审计后会跳转到Modified状态
                    entry.Reload();
                    AuditPropertySetter.SetSoftDeleteProperty(entity);
                }

                break;
            case EntityState.Modified: //修改状态：该实体正在由上下文跟踪并存在于数据库中，其部分或全部属性值已被修改
                // 只要有一个属性被修改了，且值不由数据库生成
                if (entry.Properties.Any(p => p is { IsModified: true, Metadata.ValueGenerated: ValueGenerated.Never }))
                {
                    AuditPropertySetter.SetModificationProperties(entity);
                }

                break;
            case EntityState.Added: //新增状态：上下文正在跟踪实体，但数据库中尚不存在该实体。
                CheckAndSetId(entry);
                if (entity is IAggregateRoot aggregate)
                {
                    // 初始化并发戳
                    aggregate.ConcurrencyStamp = SimpleGuidGenerator.Instance.Generate();
                }

                AuditPropertySetter.SetCreationProperties(entity);
                AuditPropertySetter.SetModificationProperties(entity);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected virtual void CheckAndSetId(EntityEntry entry)
    {
        if (!ShouldSetId(entry)) return;

        if (entry.Entity is IEntity<Guid> entityWithGuidId)
        {
            EntityHelper.TrySetId(entityWithGuidId,
                () => ServiceProvider.GetRequiredService<IIdGenerator<Guid>>().Generate(), true);
        }

        if (entry.Entity is IEntity<long> entityWithLongId)
        {
            EntityHelper.TrySetId(entityWithLongId,
                () => ServiceProvider.GetRequiredService<IIdGenerator<long>>().Generate(), true);
        }
    }


    protected virtual bool ShouldSetId(EntityEntry entry)
    {
        if (entry.Entity is not IEntity entity) return false;

        if (!entity.IsTransient()) return false;

        var idProperty = entry.Property(nameof(IEntity<AnyKey>.Id)).Metadata.PropertyInfo;

        if (idProperty == null) return false;

        var attr = ReflectionHelper.GetAttributeOrNull<DatabaseGeneratedAttribute>(idProperty);
        return attr == null || attr.DatabaseGeneratedOption == DatabaseGeneratedOption.None;
    }

    protected virtual void TrySetDatabaseProvider(ModelBuilder modelBuilder)
    {
        DatabaseProvider? provider = Database.ProviderName switch
        {
            "Microsoft.EntityFrameworkCore.SqlServer" => DatabaseProvider.SqlServer,
            "Pomelo.EntityFrameworkCore.MySql" => DatabaseProvider.MySql,
            _ => null
        };

        if (provider != null)
        {
            modelBuilder.SetDatabaseProvider(provider.Value);
        }
    }

    protected virtual void ConfigureBaseProperties<TEntity>(ModelBuilder modelBuilder,
        IMutableEntityType mutableEntityType) where TEntity : class
    {
        /*
         * 如果是附庸实体，意味着其作为一个字段直接由另一个实体持有，它没有自己的表。
         * 它会伴随所属实体一同被创建更新或销毁，在DDD中很生动的对应着值对象。
         */
        if (mutableEntityType.IsOwned()) return;

        if (!typeof(TEntity).IsAssignableTo(typeof(IEntity))) return;

        modelBuilder.Entity<TEntity>()
            .TryConfigureCreator()
            .TryConfigureModifier()
            .TryConfigureSoftDelete()
            .TryConfigureAggregateRoot()
            .TryConfigureExtraProperties();

        // 配置全局过滤器
        ConfigureGlobalFilters<TEntity>(modelBuilder, mutableEntityType);
    }

    protected virtual void ConfigureValueConverter(ModelBuilder modelBuilder, IMutableEntityType mutableEntityType)
    {
        if (mutableEntityType.BaseType != null) return;
        if (mutableEntityType.IsOwned()) return;

        var entityType = mutableEntityType.ClrType;
        if (entityType.IsDefined(typeof(OwnedAttribute), true)) return;

        var properties = mutableEntityType.GetProperties().ToList();

        foreach (var property in properties.Where(p =>
                     p.PropertyInfo?.PropertyType.IsAssignableTo<Enumeration>() ?? false))
        {
            var propertyType = property.PropertyInfo!.PropertyType;
            var converterType = typeof(FakeEnumerationValueConverter<>).MakeGenericType(propertyType);
            var converterInstance = ReflectionHelper.CreateInstance(converterType, [null]).To<ValueConverter>();
            modelBuilder.Entity(entityType).Property(property.Name).HasConversion(converterInstance);
        }

        if (!entityType.IsDefined(typeof(DisableClockNormalizationAttribute)))
        {
            var dateTimeProperties = properties
                .Where(p =>
                {
                    var propertyInfo = p.PropertyInfo;
                    if (propertyInfo == null) return false;
                    if (!propertyInfo.CanWrite) return false;
                    if (ReflectionHelper.GetAttributeOrNull<DisableClockNormalizationAttribute>(
                            propertyInfo, includeDeclaringType: true) != null) return false;
                    return propertyInfo.PropertyType == typeof(DateTime) ||
                           propertyInfo.PropertyType == typeof(DateTime?);
                });

            foreach (var property in dateTimeProperties)
            {
                modelBuilder.Entity(entityType).Property(property.Name)
                    .HasConversion(property.ClrType == typeof(DateTime)
                        ? new FakeDateTimeValueConverter(FakeClock)
                        : new FakeNullableDateTimeValueConverter(FakeClock));
            }
        }
    }

    protected virtual void ConfigureGlobalFilters<TEntity>(ModelBuilder modelBuilder,
        IMutableEntityType mutableEntityType)
        where TEntity : class
    {
        if (!ShouldFilterEntity<TEntity>(mutableEntityType)) return;

        Expression<Func<TEntity, bool>>? filter = null;

        if (typeof(TEntity).IsAssignableTo(typeof(ISoftDelete)))
        {
            filter = entity => !DataFilter.IsEnabled<ISoftDelete>() ||
                               !EF.Property<bool>(entity, nameof(ISoftDelete.IsDeleted));
        }

        if (filter == null) return;

#pragma warning disable EF1001
        // 保留已有的过滤器
        if (modelBuilder.Entity<TEntity>().Metadata.FindAnnotation(CoreAnnotationNames.QueryFilter) is
            { Value: Expression<Func<TEntity, bool>> existingFilter })
        {
            filter = ExpressionHelper.Combine(filter, existingFilter);
        }
#pragma warning restore EF1001
        modelBuilder.Entity<TEntity>().HasQueryFilter(filter);
    }

    protected virtual bool ShouldFilterEntity<TEntity>(IMutableEntityType mutableEntityType) where TEntity : class
    {
        if (mutableEntityType.BaseType != null) return false;

        if (typeof(TEntity).IsAssignableTo<ISoftDelete>()) return true;

        return false;
    }
}