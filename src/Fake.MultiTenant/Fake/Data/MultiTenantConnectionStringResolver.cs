using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fake.MultiTenant;
using Microsoft.Extensions.Configuration;

namespace Fake.Data;

public class MultiTenantConnectionStringResolver(
    ICurrentTenant currentTenant,
    ITenantStore tenantStore,
    IConfiguration configuration)
    : DefaultConnectionStringResolver(configuration)
{
    public override async Task<string> ResolveAsync(string connectionStringName)
    {
        if (connectionStringName.IsNullOrEmpty())
        {
            return await base.ResolveAsync(ConnectionStrings.DefaultConnectionStringName);
        }
        
        var tenantId = currentTenant.Id;
        if (tenantId != null)
        {
            var tenant = await tenantStore.FirstOrDefaultAsync(tenantId.Value);
            if (tenant != null && tenant.ConnectionStrings != null)
            {
                var connectionString = tenant.ConnectionStrings.GetOrDefault(connectionStringName);
                if (!connectionString.IsNullOrEmpty())
                {
                    return connectionString!;
                }
            }
        }
        
        // 无法从租户中解析到连接字符串，回退到默认逻辑
        return await base.ResolveAsync(connectionStringName);
    }
}