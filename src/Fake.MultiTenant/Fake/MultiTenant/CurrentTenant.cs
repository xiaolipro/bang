using System;
using Fake.Threading;

namespace Fake.MultiTenant;

public class CurrentTenant(IAmbientScopeProvider<TenantInfo> ambientScopeProvider) : ICurrentTenant
{
    private const string CurrentTenantContextKey = "Fake.MultiTenant.CurrentTenantScope";
    private TenantInfo? Current => ambientScopeProvider.GetValue(CurrentTenantContextKey);

    public bool IsAvailable => Id.HasValue;
    public Guid? Id => Current?.TenantId;
    public string? Name => Current?.Name;

    public IDisposable Change(TenantInfo tenantInfo)
    {
        return ambientScopeProvider.BeginScope(CurrentTenantContextKey, tenantInfo);
    }
}