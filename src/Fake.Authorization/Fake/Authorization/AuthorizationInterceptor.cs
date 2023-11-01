﻿using System.Threading.Tasks;
using Fake.Authorization.Localization;
using Fake.DynamicProxy;

namespace Fake.Authorization;

public class AuthorizationInterceptor : IFakeInterceptor
{
    private readonly IMethodAuthorizationService _methodAuthorizationService;

    public AuthorizationInterceptor(IMethodAuthorizationService methodAuthorizationService)
    {
        _methodAuthorizationService = methodAuthorizationService;
    }

    public virtual async Task InterceptAsync(IFakeMethodInvocation invocation)
    {
        if (await _methodAuthorizationService.IsGrantedAsync(invocation.Method))
        {
            await invocation.ProcessAsync();
            return;
        }

        throw new FakeAuthorizationException(FakeAuthorizationResource.GivenPolicyHasNotGranted);
    }
}