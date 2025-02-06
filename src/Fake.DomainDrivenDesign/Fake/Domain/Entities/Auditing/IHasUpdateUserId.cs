namespace Fake.Domain.Entities.Auditing;

/*
 * tips：在设计上，希望规避可空值类型
 */

public interface IHasUpdateUserId
{
    /// <summary>
    /// 更新用户Id
    /// </summary>
    Guid UpdateUserId { get; }
}

public interface IHasUpdateUserId<out TUserId> where TUserId : struct
{
    /// <summary>
    /// 更新用户Id
    /// </summary>
    TUserId UpdateUserId { get; }
}