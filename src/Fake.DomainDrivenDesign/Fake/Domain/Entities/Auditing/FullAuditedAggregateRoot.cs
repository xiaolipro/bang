namespace Fake.Domain.Entities.Auditing;

[Serializable]
public abstract class FullAuditedAggregateRoot<TKey> : AggregateRoot<TKey>, IFullAuditedEntity where TKey : struct
{
    public virtual Guid CreateUserId { get; set; }
    public virtual DateTime CreateTime { get; set; }
    public virtual Guid UpdateUserId { get; set; }
    public virtual DateTime UpdateTime { get; set; }
    public virtual bool IsDeleted { get; set; }
}

public abstract class FullAuditedAggregateRoot<TKey, TUserKey> : AggregateRoot<TKey>, IFullAuditedEntity<TUserKey>
    where TKey : struct 
    where TUserKey : struct
{
    public virtual TUserKey CreateUserId { get; set; }
    public virtual DateTime CreateTime { get; set; }
    public virtual TUserKey UpdateUserId { get; set; }
    public virtual DateTime UpdateTime { get; set; }
    public virtual bool IsDeleted { get; set; }
}