using Fake.Application;

namespace Fake.TenantManagement.Application.Contracts.Services;

public interface ITenantAppService : IApplicationService
{
    Task<string?> GetDefaultConnectionStringAsync(Guid id);

    Task UpdateDefaultConnectionStringAsync(Guid id, string defaultConnectionString);

    Task DeleteDefaultConnectionStringAsync(Guid id);
}