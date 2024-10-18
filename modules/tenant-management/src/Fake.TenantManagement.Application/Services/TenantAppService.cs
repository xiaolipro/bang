using Fake.Application.Dtos;
using Fake.Domain.Exceptions;
using Fake.TenantManagement.Application.Contracts.Dtos;
using Fake.TenantManagement.Application.Contracts.Services;
using Fake.TenantManagement.Domain.Dtos;
using Fake.TenantManagement.Domain.Localization;
using Fake.TenantManagement.Domain.Services;
using Fake.TenantManagement.Domain.TenantAggregate;

namespace Fake.TenantManagement.Application.Services;

public class TenantAppService(ITenantRepository tenantRepository, TenantManager tenantManager)
    : TenantManagementAppServiceBase, ITenantAppService
{
    public async Task<TenantItemResponse> GetAsync(Guid id)
    {
        var tenant = await tenantRepository.FirstOrDefaultAsync(id);

        if (tenant == null) throw new DomainException(L[FakeTenantManagementResource.TenantNotExists, id]);

        return ObjectMapper.Map<Tenant, TenantItemResponse>(tenant);
    }

    public async Task<PagedResult<TenantItemResponse>> GetPagedListAsync(GetTenantPagedRequest input)
    {
        var query = ObjectMapper.Map<GetTenantPagedRequest, GetTenantPagedQuery>(input);
        var res = await tenantRepository.GetPagedListAsync(query);

        return new PagedResult<TenantItemResponse>(
            res.TotalCount,
            ObjectMapper.Map<IReadOnlyList<Tenant>, IReadOnlyList<TenantItemResponse>>(res.Items)
        );
    }

    public async Task<string> GetDefaultConnectionStringAsync(Guid id)
    {
        var tenant = await tenantRepository.FirstAsync(id);

        return tenant.GetDefaultConnectionString();
    }

    public Task UpdateDefaultConnectionStringAsync(Guid id, string defaultConnectionString)
    {
        throw new NotImplementedException();
    }

    public Task DeleteDefaultConnectionStringAsync(Guid id)
    {
        throw new NotImplementedException();
    }
}