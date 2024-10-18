using Fake.Application.Dtos;
using Fake.Domain.Repositories;
using Fake.TenantManagement.Domain.Dtos;

namespace Fake.TenantManagement.Domain.TenantAggregate;

public interface ITenantRepository : IRepository<Tenant, Guid>
{
    Task<Tenant?> FindByNameAsync(string normalizedName);
    Task<PagedResult<Tenant>> GetPagedListAsync(GetTenantPagedQuery query);
}