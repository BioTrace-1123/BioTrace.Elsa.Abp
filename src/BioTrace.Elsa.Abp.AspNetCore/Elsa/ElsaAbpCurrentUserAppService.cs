using BioTrace.Elsa.Abp.Security;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.TenantManagement;

namespace BioTrace.Elsa.Abp.Elsa;

[Authorize]
public class ElsaAbpCurrentUserAppService : ApplicationService, IElsaAbpCurrentUserAppService
{
    private readonly IElsaAbpEffectivePermissionsProvider _effectivePermissionsProvider;
    private readonly ITenantRepository _tenantRepository;

    public ElsaAbpCurrentUserAppService(
        IElsaAbpEffectivePermissionsProvider effectivePermissionsProvider,
        ITenantRepository tenantRepository)
    {
        _effectivePermissionsProvider = effectivePermissionsProvider;
        _tenantRepository = tenantRepository;
    }

    public virtual async Task<ElsaAbpCurrentUserDto> GetAsync()
    {
        var permissions = await _effectivePermissionsProvider.GetElsaPermissionsAsync();

        return new ElsaAbpCurrentUserDto
        {
            UserId = CurrentUser.Id,
            UserName = CurrentUser.UserName,
            Email = CurrentUser.Email,
            TenantId = CurrentUser.TenantId,
            // Host users may pass __tenant for impersonation; only expose tenant for real tenant users.
            TenantName = await ResolveTenantNameAsync(),
            Roles = CurrentUser.Roles.ToList(),
            Permissions = permissions.ToList()
        };
    }

    protected virtual async Task<string?> ResolveTenantNameAsync()
    {
        if (!CurrentUser.TenantId.HasValue)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(CurrentTenant.Name))
        {
            return CurrentTenant.Name;
        }

        var tenant = await _tenantRepository.FindAsync(CurrentUser.TenantId.Value);
        return tenant?.Name;
    }
}
