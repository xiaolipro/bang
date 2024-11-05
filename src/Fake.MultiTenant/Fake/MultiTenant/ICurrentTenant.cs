using System;

namespace Fake.MultiTenant;

public interface ICurrentTenant
{
    bool IsAvailable { get; }

    Guid? Id { get; }

    string? Name { get; }

    IDisposable Change(TenantInfo tenantInfo);
}