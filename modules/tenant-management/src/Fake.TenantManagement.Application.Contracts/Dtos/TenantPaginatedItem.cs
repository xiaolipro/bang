namespace Fake.TenantManagement.Application.Contracts.Dtos;

public class TenantPaginatedItem
{
    public Guid Id { get; set; }

    public required string Name { get; set; }
}