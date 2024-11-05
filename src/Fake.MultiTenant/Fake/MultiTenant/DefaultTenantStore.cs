using System;
using System.Threading.Tasks;

namespace Fake.MultiTenant;

public class DefaultTenantStore: ITenantStore
{
    public Task<TenantConfiguration?> FirstOrDefaultAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<TenantConfiguration?> FirstOrDefaultAsync(string code)
    {
        throw new NotImplementedException();
    }
}