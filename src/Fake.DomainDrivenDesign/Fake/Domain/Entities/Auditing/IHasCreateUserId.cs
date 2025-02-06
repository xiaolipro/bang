namespace Fake.Domain.Entities.Auditing;

public interface IHasCreateUserId
{
    /// <summary>
    /// 创建用户Id
    /// </summary>
    Guid CreateUserId { get; }
}

public interface IHasCreateUserId<out TUserId> where TUserId : struct
{
    /// <summary>
    /// 创建用户Id
    /// </summary>
    TUserId CreateUserId { get; }
}