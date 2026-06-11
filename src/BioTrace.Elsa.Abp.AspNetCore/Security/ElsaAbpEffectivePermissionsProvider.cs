using System.Security.Claims;
using BioTrace.Elsa.Abp.Permissions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Security.Claims;
using Volo.Abp.Users;

namespace BioTrace.Elsa.Abp.Security;

public class ElsaAbpEffectivePermissionsProvider : IElsaAbpEffectivePermissionsProvider, ITransientDependency
{
    public const string HttpContextItemKey = "ElsaAbp:EffectivePermissions";

    private readonly IPermissionManager _permissionManager;
    private readonly IPermissionChecker _permissionChecker;
    private readonly IElsaAbpPermissionMapper _permissionMapper;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentTenant _currentTenant;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptions<ElsaAbpOptions> _options;

    public ElsaAbpEffectivePermissionsProvider(
        IPermissionManager permissionManager,
        IPermissionChecker permissionChecker,
        IElsaAbpPermissionMapper permissionMapper,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IHttpContextAccessor httpContextAccessor,
        IOptions<ElsaAbpOptions> options)
    {
        _permissionManager = permissionManager;
        _permissionChecker = permissionChecker;
        _permissionMapper = permissionMapper;
        _currentUser = currentUser;
        _currentTenant = currentTenant;
        _httpContextAccessor = httpContextAccessor;
        _options = options;
    }

    public virtual async Task<IReadOnlyList<string>> GetElsaPermissionsAsync(
        ClaimsPrincipal? principal = null,
        CancellationToken cancellationToken = default)
    {
        if (_httpContextAccessor.HttpContext?.Items.TryGetValue(HttpContextItemKey, out var cached) == true
            && cached is IReadOnlyList<string> cachedList)
        {
            return cachedList;
        }

        principal ??= _httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true && !_currentUser.IsAuthenticated)
        {
            return Array.Empty<string>();
        }

        var grantedAbp = await GetGrantedAbpPermissionNamesAsync(cancellationToken);
        var elsaPermissions = _permissionMapper.MapToElsaPermissions(grantedAbp);

        if (_httpContextAccessor.HttpContext != null)
        {
            _httpContextAccessor.HttpContext.Items[HttpContextItemKey] = elsaPermissions;
        }

        return elsaPermissions;
    }

    protected virtual async Task<List<string>> GetGrantedAbpPermissionNamesAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.IsAuthenticated
            && await IsAdminGrantedAsync(cancellationToken))
        {
            return [AbpElsaPermissions.Admin];
        }

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (_currentUser.Id.HasValue)
        {
            var userPermissions = await _permissionManager.GetAllAsync(
                UserPermissionValueProvider.ProviderName,
                _currentUser.Id.Value.ToString());
            AddGrantedNames(userPermissions, result);
        }

        foreach (var role in _currentUser.Roles)
        {
            var rolePermissions = await _permissionManager.GetAllAsync(
                RolePermissionValueProvider.ProviderName,
                role);
            AddGrantedNames(rolePermissions, result);
        }

        return result.ToList();
    }

    protected virtual async Task<bool> IsAdminGrantedAsync(CancellationToken cancellationToken)
    {
        // Host users may pass __tenant to view a tenant while remaining host-side; evaluate Admin at host scope.
        if (IsHostUser())
        {
            using (_currentTenant.Change(null))
            {
                return await _permissionChecker.IsGrantedAsync(AbpElsaPermissions.Admin);
            }
        }

        return await _permissionChecker.IsGrantedAsync(AbpElsaPermissions.Admin);
    }

    protected virtual bool IsHostUser()
    {
        return _currentUser.IsAuthenticated && !_currentUser.TenantId.HasValue;
    }

    protected virtual void AddGrantedNames(
        List<PermissionWithGrantedProviders> permissions,
        HashSet<string> target)
    {
        foreach (var permission in permissions)
        {
            if (permission.IsGranted)
            {
                target.Add(permission.Name);
            }
        }
    }
}
