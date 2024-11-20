using System;
using System.Linq;
using System.Security.Claims;
using Fake.Security.Claims;

namespace Fake.Users;

public class CurrentUser(ICurrentPrincipalAccessor currentPrincipalAccessor) : ICurrentUser
{
    /*
     * see: public virtual bool IsAuthenticated => !string.IsNullOrEmpty(this.m_authenticationType);
     */
    public virtual bool IsAuthenticated => currentPrincipalAccessor.Principal?.Identity?.IsAuthenticated ?? false;
    public virtual Guid? Id => currentPrincipalAccessor.Principal?.FindUserId();
    public Guid? TenantId => currentPrincipalAccessor.Principal?.FindTenantId();
    public virtual string? UserName => this.FindClaimValueOrNull(FakeClaimTypes.UserName);
    public string? Email=> this.FindClaimValueOrNull(FakeClaimTypes.Email);
    public virtual string[] Roles => this.FindClaimValues(FakeClaimTypes.Role);

    public virtual Claim? FindClaimOrNull(string claimType)
    {
        return currentPrincipalAccessor
            .Principal?
            .Claims
            .FirstOrDefault(c => c.Type == claimType);
    }

    public Claim[] FindClaims(string claimType)
    {
        return currentPrincipalAccessor
            .Principal?
            .Claims
            .Where(c => c.Type == claimType)
            .ToArray() ?? [];
    }
}