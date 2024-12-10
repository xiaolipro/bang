using Fake.Domain.Repositories;

namespace Fake.TenantManagement.Domain.TenantAggregate;

public interface ITenantRepository : IRepository<Tenant>
{
    // Task<Tenant?> FindByNameAsync(string name);
    // Task<PagedResult<Tenant>> GetPagedListAsync(GetTenantPagedQuery query);
}