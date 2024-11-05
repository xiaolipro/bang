using Fake.DomainDrivenDesign.Repositories.EntityFrameWorkCore;
using Fake.TenantManagement.Domain.TenantAggregate;

namespace Fake.TenantManagement.Infrastructure.Repositories;

public class TenantRepository : EfCoreRepository<TenantManagementDbContext, Tenant>, ITenantRepository
{
}