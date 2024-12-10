using AutoMapper;
using Fake.TenantManagement.Application.Contracts.Dtos;
using Fake.TenantManagement.Domain.TenantAggregate;

namespace Fake.TenantManagement.Application.AutoMapper;

public class TenantAutoMapperProfile : Profile
{
    public TenantAutoMapperProfile()
    {
        CreateMap<Tenant, TenantPagedItem>();
    }
}