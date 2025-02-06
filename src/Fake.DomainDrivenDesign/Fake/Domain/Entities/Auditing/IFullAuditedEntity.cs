namespace Fake.Domain.Entities.Auditing;

public interface IFullAuditedEntity : IEntity, IHasCreateUserId, IHasUpdateUserId
    , IHasCreateTime, IHasUpdateTime, ISoftDelete;

public interface IFullAuditedEntity<out TUserId> : IEntity, IHasCreateUserId<TUserId>, IHasUpdateUserId<TUserId>
    , IHasCreateTime, IHasUpdateTime, ISoftDelete where TUserId : struct;