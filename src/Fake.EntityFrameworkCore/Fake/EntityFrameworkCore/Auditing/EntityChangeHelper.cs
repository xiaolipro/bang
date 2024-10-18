using Fake.Auditing;
using Fake.Json;
using Microsoft.Extensions.Options;

namespace Fake.EntityFrameworkCore.Auditing;

public class EntityChangeHelper(
    IOptions<FakeAuditingOptions> options,
    IFakeClock clock,
    IFakeJsonSerializer jsonSerializer,
    IAuditingHelper auditingHelper,
    IAuditingManager auditingManager)
    : IEntityChangeHelper
{
    protected IAuditingManager AuditingManager { get; } = auditingManager;
    protected IFakeJsonSerializer JsonSerializer { get; } = jsonSerializer;
    protected FakeAuditingOptions Options { get; } = options.Value;
    protected IAuditingHelper AuditingHelper { get; } = auditingHelper;
    protected IFakeClock Clock { get; } = clock;

    #region 创建实体变更集合

    public List<EntityChangeInfo>? CreateChangeList(IEnumerable<EntityEntry> entityEntries)
    {
        if (AuditingManager.Current == null) return null;

        var changes = new List<EntityChangeInfo>();

        foreach (var entry in entityEntries)
        {
            if (!ShouldSaveEntityChange(entry)) continue;

            var change = CreateEntityChangeOrNull(entry);

            if (change != null) changes.Add(change);
        }

        return changes;
    }


    protected virtual EntityChangeInfo? CreateEntityChangeOrNull(EntityEntry entry)
    {
        var entity = entry.Entity;

        EntityChangeType changeType;
        switch (entry.State)
        {
            case EntityState.Added:
                changeType = EntityChangeType.Created;
                break;
            case EntityState.Deleted:
                changeType = EntityChangeType.Deleted;
                break;
            case EntityState.Modified:
                changeType = IsSoftDeleted(entry) ? EntityChangeType.Deleted : EntityChangeType.Updated;
                break;
            case EntityState.Detached:
            case EntityState.Unchanged:
            default:
                return null;
        }

        var entityId = GetEntityIdAsString(entity);

        if (changeType != EntityChangeType.Created) return null;

        var entityChange = new EntityChangeInfo
        {
            ChangeType = changeType,
            EntityEntry = entry,
            EntityId = entityId,
            EntityTypeFullName = entity.GetType().FullName,
            PropertyChanges = GetPropertyChanges(entry),
        };

        return entityChange;
    }

    protected virtual List<EntityPropertyChangeInfo> GetPropertyChanges(EntityEntry entityEntry)
    {
        var propertyChanges = new List<EntityPropertyChangeInfo>();
        var properties = entityEntry.Metadata.GetProperties();
        var isCreated = entityEntry.State == EntityState.Added;
        var isDeleted = IsDeleted(entityEntry);

        if (IsSoftDeleted(entityEntry)) return propertyChanges;

        foreach (var property in properties)
        {
            var propertyEntry = entityEntry.Property(property.Name);

            if (ShouldSaveEntityPropertyChange(propertyEntry))
            {
                propertyChanges.Add(new EntityPropertyChangeInfo
                {
                    NewValue = isDeleted
                        ? null
                        : JsonSerializer.Serialize(propertyEntry.CurrentValue!)
                            .TruncateWithSuffix(Options.EntityChangeOptions.ValueMaxLength),
                    OriginalValue = isCreated
                        ? null
                        : JsonSerializer.Serialize(propertyEntry.OriginalValue!)
                            .TruncateWithSuffix(Options.EntityChangeOptions.ValueMaxLength),
                    PropertyName = property.Name,
                    PropertyTypeFullName = property.ClrType.FullName!
                });
            }
        }

        return propertyChanges;
    }

    protected virtual bool IsDeleted(EntityEntry entityEntry)
    {
        return entityEntry.State == EntityState.Deleted || IsSoftDeleted(entityEntry);
    }

    protected virtual bool IsSoftDeleted(EntityEntry entityEntry)
    {
        var entity = entityEntry.Entity;
        return entity is ISoftDelete && entity.To<ISoftDelete>().IsDeleted;
    }


    protected virtual string GetEntityIdAsString(object entityAsObj)
    {
        if (entityAsObj is not IEntity entity)
        {
            throw new FakeException(
                $"实体必须实现{typeof(IEntity).AssemblyQualifiedName}接口！给定的实体并没有实现它: {entityAsObj.GetType().AssemblyQualifiedName}");
        }

        return entity.GetKeys().JoinAsString(",");
    }

    protected virtual bool ShouldSaveEntityChange(EntityEntry entry)
    {
        if (!Options.EntityChangeOptions.IsEnabled) return false;
        if (entry.State.IsIn(EntityState.Detached, EntityState.Unchanged)) return false;

        var entityType = entry.Metadata.ClrType;

        if (!EntityHelper.IsEntity(entityType)) return false;

        if (AuditingHelper.IsAuditEntity(entityType)) return true;

        return false;
    }

    protected virtual bool ShouldSaveEntityPropertyChange(PropertyEntry propertyEntry)
    {
        if (propertyEntry.Metadata.Name.IsIn(Options.EntityChangeOptions.IgnoreProperties)) return false;

        var propertyInfo = propertyEntry.Metadata.FieldInfo;
        if (propertyInfo == null) return false;
        if (propertyInfo.IsDefined(typeof(DisableAuditingAttribute), true)) return false;

        if (propertyEntry.OriginalValue is ExtraProperties originalValue &&
            propertyEntry.CurrentValue is ExtraProperties currentValue)
        {
            if (originalValue.IsNullOrEmpty() && currentValue.IsNullOrEmpty()) return false;
            if (!originalValue.Select(x => x.Key).SequenceEqual(currentValue.Select(x => x.Key))) return true;
            if (!originalValue.Select(x => x.Value).SequenceEqual(currentValue.Select(x => x.Value))) return true;
            return false;
        }

        return !(propertyEntry.OriginalValue?.Equals(propertyEntry.CurrentValue) ?? propertyEntry.CurrentValue == null);
    }

    #endregion


    public void UpdateChangeList(List<EntityChangeInfo> entityChanges)
    {
        var auditLog = AuditingManager.Current?.Log;
        if (auditLog == null) return;

        foreach (var entityChange in entityChanges)
        {
            entityChange.ChangeTime = GetChangeTime(entityChange);

            var entityEntry = entityChange.EntityEntry.To<EntityEntry>();
            entityChange.EntityId = GetEntityIdAsString(entityEntry.Entity);

            var foreignKeys = entityEntry.Metadata.GetForeignKeys();

            foreach (var foreignKey in foreignKeys)
            {
                foreach (var property in foreignKey.Properties)
                {
                    var propertyEntry = entityEntry.Property(property.Name);
                    var propertyChange =
                        entityChange.PropertyChanges.FirstOrDefault(pc => pc.PropertyName == property.Name);

                    if (propertyChange == null)
                    {
                        if (!(propertyEntry.OriginalValue?.Equals(propertyEntry.CurrentValue) ??
                              propertyEntry.CurrentValue == null))
                        {
                            // Add foreign key
                            entityChange.PropertyChanges.Add(new EntityPropertyChangeInfo
                            {
                                NewValue = JsonSerializer.Serialize(propertyEntry.CurrentValue!),
                                OriginalValue = JsonSerializer.Serialize(propertyEntry.OriginalValue!),
                                PropertyName = property.Name,
                                PropertyTypeFullName = property.ClrType.FullName!
                            });
                        }

                        continue;
                    }

                    if (propertyChange.OriginalValue == propertyChange.NewValue)
                    {
                        var newValue = JsonSerializer.Serialize(propertyEntry.CurrentValue!);
                        if (newValue == propertyChange.NewValue)
                        {
                            // No change
                            entityChange.PropertyChanges.Remove(propertyChange);
                        }
                        else
                        {
                            // Update foreign key
                            propertyChange.NewValue =
                                newValue.TruncateWithSuffix(Options.EntityChangeOptions.ValueMaxLength);
                        }
                    }
                }
            }
        }

        auditLog.EntityChanges.AddRange(entityChanges);
    }

    protected virtual DateTime GetChangeTime(EntityChangeInfo entityChange)
    {
        var entity = entityChange.EntityEntry.To<EntityEntry>().Entity;
        switch (entityChange.ChangeType)
        {
            case EntityChangeType.Created:
                return (entity as IHasCreateTime)?.CreateTime ?? Clock.Now;
            case EntityChangeType.Updated:
                return (entity as IHasUpdateTime)?.UpdateTime ?? Clock.Now;
            default:
                throw new FakeException($"Unknown {nameof(EntityChangeInfo)}: {entityChange}");
        }
    }
}