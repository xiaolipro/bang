using System.Security.Claims;

namespace Fake.Users;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>
    /// 用户id
    /// </summary>
    Guid? Id { get; }
    
    /// <summary>
    /// 租户id
    /// </summary>
    Guid? TenantId { get;}

    /// <summary>
    /// 获取当前用户的名称。
    /// </summary>
    string? UserName { get; }

    string? Email { get; }
    string[] Roles { get; }

    Claim? FindClaimOrNull(string claimType);

    Claim[] FindClaims(string claimType);
    
    /// <summary>
    /// 当用户id不是Guid类型时，可以通过此方法获取用户id
    /// </summary>
    /// <typeparam name="TUserId"></typeparam>
    /// <returns></returns>
    TUserId? GetUserIdOrNull<TUserId>() where TUserId : struct;
}