namespace Fake.Domain.Entities.Auditing;

/// <summary>
/// 完全审计实体
/// </summary>
/// <typeparam name="TKey">id类型</typeparam>
[Serializable]
public abstract class FullAuditedEntity<TKey> : Entity<TKey>, IFullAuditedEntity where TKey : struct
{
    public virtual Guid CreateUserId { get; set; }
    public virtual DateTime CreateTime { get; set; }
    public virtual Guid UpdateUserId { get; set; }
    public virtual DateTime UpdateTime { get; set; }
    public virtual bool IsDeleted { get; set; }
}

[Serializable]
public abstract class FullAuditedEntity<TKey, TUserId> : Entity<TKey>, IFullAuditedEntity<TUserId>
    where TKey : struct
    where TUserId : struct
{
    public virtual TUserId CreateUserId { get; set; }
    public virtual DateTime CreateTime { get; set; }
    public virtual TUserId UpdateUserId { get; set; }
    public virtual DateTime UpdateTime { get; set; }
    public virtual bool IsDeleted { get; set; }
}