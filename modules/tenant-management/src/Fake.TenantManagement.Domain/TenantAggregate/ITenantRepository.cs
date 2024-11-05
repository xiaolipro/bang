using Fake.Domain.Repositories;

namespace Fake.TenantManagement.Domain.TenantAggregate;

public interface ITenantRepository : IRepository<Tenant>
{
    // Task<Tenant?> FindByNameAsync(string name);
    // Task<PaginatedResult<Tenant>> GetPagedListAsync(GetTenantPagedQuery query);
}