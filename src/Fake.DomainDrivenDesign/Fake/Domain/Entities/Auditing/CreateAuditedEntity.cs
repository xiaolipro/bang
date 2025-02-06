namespace Fake.Domain.Entities.Auditing;

/// <summary>
/// 创建审计实体
/// </summary>
/// <typeparam name="TKey"></typeparam>
[Serializable]
public abstract class CreateAuditedEntity<TKey> : Entity<TKey>, IHasCreateUserId, IHasCreateTime where TKey : struct
{
    public virtual Guid CreateUserId { get; set; }
    public virtual DateTime CreateTime { get; set; }
}

[Serializable]
public abstract class CreateAuditedEntity<TKey, TUserId> : Entity<TKey>, IHasCreateUserId<TUserId>, IHasCreateTime
    where TKey : struct
    where TUserId : struct
{
    public virtual TUserId CreateUserId { get; set; }
    public virtual DateTime CreateTime { get; set; }
}