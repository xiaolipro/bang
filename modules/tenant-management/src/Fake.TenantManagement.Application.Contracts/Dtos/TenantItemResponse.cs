namespace Fake.TenantManagement.Application.Contracts.Dtos;

public class TenantItemResponse
{
    public Guid Id { get; set; }

    public required string Name { get; set; }
}