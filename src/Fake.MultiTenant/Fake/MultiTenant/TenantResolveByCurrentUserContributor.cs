using System.Threading.Tasks;
using Fake.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Fake.MultiTenant;

public class TenantResolveByCurrentUserContributor : ITenantResolveContributor
{
    public string Name => "CurrentUser";

    public Task ResolveAsync(TenantResolveContext context)
    {
        var currentUser = context.ServiceProvider.GetRequiredService<ICurrentUser>();
        if (currentUser.IsAuthenticated)
        {
            context.TenantId = currentUser.TenantId;
        }
        
        return Task.CompletedTask;
    }
}