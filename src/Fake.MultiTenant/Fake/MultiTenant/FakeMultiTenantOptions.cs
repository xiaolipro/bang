using System.Collections.Generic;

namespace Fake.MultiTenant;

public class FakeMultiTenantOptions
{
    public bool IsEnabled { get; set; }

    public List<ITenantResolveContributor> TenantResolverContributors { get; } = [new TenantResolveByCurrentUserContributor()];
}