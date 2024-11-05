using System.Linq;
using System.Reflection;
using Fake.Authorization.Localization;
using Fake.DynamicProxy;
using Fake.Security.Claims;
using Microsoft.Extensions.Localization;

namespace Fake.Authorization;

public class AuthorizationInterceptor(
    ICurrentPrincipalAccessor currentPrincipalAccessor,
    IAuthorizationService authorizationService,
    IAuthorizationPolicyProvider authorizationPolicyProvider,
    IStringLocalizer<FakeAuthorizationResource> localizer) : IFakeInterceptor
{
    public virtual async Task InterceptAsync(IFakeMethodInvocation invocation)
    {
        await AuthorizeAsync(invocation.Method);
        await invocation.ProcessAsync();
    }

    protected virtual async Task AuthorizeAsync(MethodInfo method)
    {
        if (PassThroughAuthorization(method)) return;

        var authorizeData = GetAuthorizationDataAttributes(method);

        var policy = await AuthorizationPolicy.CombineAsync(authorizationPolicyProvider, authorizeData);
        if (policy == null) return;

        if (currentPrincipalAccessor.Principal == null) return;

        // to requirement handler
        var res = await authorizationService.AuthorizeAsync(
            currentPrincipalAccessor.Principal,
            null,
            policy
        );

        if (!res.Succeeded)
        {
            throw new FakeAuthorizationException(localizer[FakeAuthorizationResource.GivenPolicyHasNotGranted]);
        }
    }

    protected virtual bool PassThroughAuthorization(MethodInfo method)
    {
        return method.GetCustomAttributes(true).OfType<IAllowAnonymous>().Any();
    }

    protected virtual IEnumerable<IAuthorizeData> GetAuthorizationDataAttributes(MethodInfo invocationMethod)
    {
        var attributes = invocationMethod.GetCustomAttributes(true)
            .OfType<IAuthorizeData>();

        if (invocationMethod is { IsPublic: true, DeclaringType: not null })
        {
            attributes = attributes.Union(
                invocationMethod.DeclaringType.GetCustomAttributes(true).OfType<IAuthorizeData>()
            );
        }

        return attributes;
    }
}