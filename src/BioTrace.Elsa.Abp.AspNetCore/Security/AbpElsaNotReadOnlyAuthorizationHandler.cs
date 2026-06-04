using BioTrace.Elsa.Abp.Permissions;
using Elsa.Workflows.Api.Requirements;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Users;

namespace BioTrace.Elsa.Abp.Security;

/// <summary>
/// Extends Elsa not-read-only checks: users with <see cref="AbpElsaPermissions.NotReadOnly"/> or
/// <see cref="AbpElsaPermissions.Admin"/> satisfy the requirement.
/// </summary>
public class AbpElsaNotReadOnlyAuthorizationHandler :
    AuthorizationHandler<NotReadOnlyRequirement, NotReadOnlyResource>,
    ITransientDependency
{
    private readonly IPermissionChecker _permissionChecker;
    private readonly ICurrentUser _currentUser;

    public AbpElsaNotReadOnlyAuthorizationHandler(
        IPermissionChecker permissionChecker,
        ICurrentUser currentUser)
    {
        _permissionChecker = permissionChecker;
        _currentUser = currentUser;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        NotReadOnlyRequirement requirement,
        NotReadOnlyResource resource)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return;
        }

        if (await _permissionChecker.IsGrantedAsync(AbpElsaPermissions.Admin)
            || await _permissionChecker.IsGrantedAsync(AbpElsaPermissions.NotReadOnly))
        {
            context.Succeed(requirement);
        }
    }
}
