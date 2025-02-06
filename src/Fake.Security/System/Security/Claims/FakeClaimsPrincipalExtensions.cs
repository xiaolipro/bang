using System.Linq;
using Fake;
using Fake.Security.Claims;

namespace System.Security.Claims;

public static class FakeClaimsPrincipalExtensions
{
    public static Guid? FindUserId(this ClaimsPrincipal principal)
    {
        ThrowHelper.ThrowIfNull(principal, nameof(principal));

        var userId = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userId == null || userId.Value.IsNullOrWhiteSpace())
        {
            return null;
        }

        if (Guid.TryParse(userId.Value, out var guid))
        {
            return guid;
        }

        return null;
    }
    
    public static Guid? FindTenantId(this ClaimsPrincipal principal)
    {
        ThrowHelper.ThrowIfNull(principal, nameof(principal));

        var tenantIdOrNull = principal.Claims?.FirstOrDefault(c => c.Type == FakeClaimTypes.TenantId);
        if (tenantIdOrNull == null || tenantIdOrNull.Value.IsNullOrWhiteSpace())
        {
            return null;
        }

        if (Guid.TryParse(tenantIdOrNull.Value, out var guid))
        {
            return guid;
        }

        return null;
    }
}