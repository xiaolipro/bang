﻿using Fake.Authorization.Permissions;

namespace Fake.Authorization;

public class PermissionRequirementHandle(IPermissionChecker permissionChecker)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var isGranted = await permissionChecker.IsGrantedAsync(requirement);
        if (isGranted) context.Succeed(requirement);
    }
}