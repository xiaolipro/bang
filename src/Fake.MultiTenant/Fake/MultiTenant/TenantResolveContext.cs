using System;

namespace Fake.MultiTenant;

public class TenantResolveContext(IServiceProvider serviceProvider)
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public Guid? TenantId { get; set; }
    
    public string? Name { get; set; }
}