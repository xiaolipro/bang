using Fake.Domain.Exceptions;
using Fake.EventBus.Local;
using Fake.TenantManagement.Domain.Events;
using Fake.TenantManagement.Domain.Localization;
using Fake.TenantManagement.Domain.TenantAggregate;

namespace Fake.TenantManagement.Domain.Services;

public class TenantManager(ITenantRepository tenantRepository, ILocalEventBus localEventBus)
    : TenantManagementDomainServiceBase
{
    public virtual async Task<Tenant> CreateAsync(string name)
    {
        ArgumentNullException.ThrowIfNull(name, nameof(name));

        var existedTenant = await tenantRepository.FirstOrDefaultAsync(x => x.Name == name);

        if (existedTenant != null)
        {
            throw new DomainException(L[FakeTenantManagementResource.TenantNameDuplicate, name]);
        }

        return new Tenant(name);
    }

    public virtual async Task ChangeNameAsync(Tenant tenant, string name)
    {
        ArgumentNullException.ThrowIfNull(tenant, nameof(tenant));
        ArgumentNullException.ThrowIfNull(name, nameof(name));

        var existedTenant = await tenantRepository.FirstOrDefaultAsync(x => x.Name == name);;

        if (existedTenant != null && existedTenant.Id != tenant.Id)
        {
            throw new DomainException(L[FakeTenantManagementResource.TenantNameDuplicate, name]);
        }

        await localEventBus.PublishAsync(new TenantNameChangedEvent(tenant.Id, tenant.Name, name));
        tenant.SetName(name);
    }
}