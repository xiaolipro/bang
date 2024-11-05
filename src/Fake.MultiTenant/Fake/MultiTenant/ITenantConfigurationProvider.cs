using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Fake.MultiTenant;

public interface ITenantConfigurationProvider
{
    Task<TenantConfiguration?> GetAsync();
}

public class TenantConfigurationProvider : ITenantConfigurationProvider
{
    private readonly IServiceScopeFactory _serviceProviderFactory;
    private readonly IOptions<FakeMultiTenantOptions> _options;
    private readonly ITenantStore _tenantStore;

    public TenantConfigurationProvider(IServiceScopeFactory serviceProviderFactory, IOptions<FakeMultiTenantOptions> options,
        ITenantStore tenantStore)
    {
        _serviceProviderFactory = serviceProviderFactory;
        _options = options;
        _tenantStore = tenantStore;
    }
    public async Task<TenantConfiguration?> GetAsync()
    {
        using var scope = _serviceProviderFactory.CreateScope();
        
        var context = new TenantResolveContext(scope.ServiceProvider);

        foreach (var tenantResolver in _options.Value.TenantResolverContributors)
        {
            await tenantResolver.ResolveAsync(context);

            if (context.TenantId != null)
            {
                var tenant = await _tenantStore.FirstOrDefaultAsync(context.TenantId.Value);
                return tenant;
            }
            
            if (!context.Name.IsNullOrWhiteSpace())
            {
                var tenant = await _tenantStore.FirstOrDefaultAsync(context.Name!);
                return tenant;
            }
        }
        
        throw new FakeException("Tenant not found");
    }
}