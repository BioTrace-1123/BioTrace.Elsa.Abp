using BioTrace.Elsa.Abp.Permissions;
using Elsa.Http;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Users;

namespace BioTrace.Elsa.Abp.Security;

[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IHttpEndpointAuthorizationHandler))]
public class AbpHttpEndpointAuthorizationHandler : IHttpEndpointAuthorizationHandler, ITransientDependency
{
    private readonly IPermissionChecker _permissionChecker;
    private readonly ICurrentUser _currentUser;

    public AbpHttpEndpointAuthorizationHandler(
        IPermissionChecker permissionChecker,
        ICurrentUser currentUser)
    {
        _permissionChecker = permissionChecker;
        _currentUser = currentUser;
    }

    public virtual async ValueTask<bool> AuthorizeAsync(AuthorizeHttpEndpointContext context)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return false;
        }

        if (await _permissionChecker.IsGrantedAsync(AbpElsaPermissions.Admin))
        {
            return true;
        }

        return await _permissionChecker.IsGrantedAsync(AbpElsaPermissions.HttpEndpoints.Invoke)
               || await _permissionChecker.IsGrantedAsync(AbpElsaPermissions.HttpEndpoints.Default);
    }
}
