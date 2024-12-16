namespace Fake.Domain.Entities.Auditing;

/// <summary>
/// 更新审计实体
/// </summary>
/// <typeparam name="TKey">id类型</typeparam>
[Serializable]
public abstract class UpdateAuditedEntity<TKey> : Entity<TKey>, IHasUpdateUserId, IHasUpdateTime
{
    public virtual Guid UpdateUserId { get; set; }
    public virtual DateTime UpdateTime { get; set; }
}