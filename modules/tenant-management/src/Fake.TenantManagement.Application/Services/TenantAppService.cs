﻿using System.Linq.Expressions;
using Fake.Application.Dtos;
using Fake.Domain.Exceptions;
using Fake.TenantManagement.Application.Contracts.Dtos;
using Fake.TenantManagement.Application.Contracts.Services;
using Fake.TenantManagement.Domain.Localization;
using Fake.TenantManagement.Domain.Services;
using Fake.TenantManagement.Domain.TenantAggregate;

namespace Fake.TenantManagement.Application.Services;

public class TenantAppService(ITenantRepository tenantRepository, TenantManager tenantManager)
    : TenantManagementAppServiceBase, ITenantAppService
{
    public async Task<TenantPaginatedItem> GetAsync(Guid id)
    {
        var existedTenant = await tenantRepository.FirstOrDefaultAsync(x => x.Id == id);

        if (existedTenant == null) throw new DomainException(L[FakeTenantManagementResource.TenantNotExists, id]);

        return ObjectMapper.Map<Tenant, TenantPaginatedItem>(existedTenant);
    }

    public async Task<PaginatedResult<TenantPaginatedItem>> GetPagedListAsync(GetTenantPaginatedRequest input)
    {
        Expression<Func<Tenant, bool>> query = x => input.Name.IsNullOrWhiteSpace() ? default : x.Name.Contains(x.Name);
        var paginatedList = await tenantRepository.GetPaginatedListAsync(query);
        var totalCount = await tenantRepository.CountAsync(query);

        return new PaginatedResult<TenantPaginatedItem>(
            totalCount,
            ObjectMapper.Map<IReadOnlyList<Tenant>, IReadOnlyList<TenantPaginatedItem>>(paginatedList)
        );
    }

    public async Task<string?> GetDefaultConnectionStringAsync(Guid id)
    {
        var tenant = await tenantRepository.FirstAsync(x => x.Id == id);

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