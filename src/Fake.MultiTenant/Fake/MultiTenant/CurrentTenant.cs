using System;
using Fake.Threading;

namespace Fake.MultiTenant;

public class CurrentTenant(IAmbientScopeProvider<TenantInfo> ambientScopeProvider) : ICurrentTenant
{
    private const string CurrentTenantContextKey = "Fake.MultiTenant.CurrentTenantScope";
    private TenantInfo? Current => ambientScopeProvider.GetValue(CurrentTenantContextKey);

    public bool IsResolved => Id == default;
    public Guid Id => Current?.TenantId ?? default;
    public string Name => Current?.Name ?? string.Empty;

    public IDisposable Change(TenantInfo tenantInfo)
    {
        return ambientScopeProvider.BeginScope(CurrentTenantContextKey, tenantInfo);
    }
}