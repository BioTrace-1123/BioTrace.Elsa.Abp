using BioTrace.Elsa.Abp.Security;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;

namespace BioTrace.Elsa.Abp.Elsa;

[Authorize]
public class ElsaAbpCurrentUserAppService : ApplicationService, IElsaAbpCurrentUserAppService
{
    private readonly IElsaAbpEffectivePermissionsProvider _effectivePermissionsProvider;

    public ElsaAbpCurrentUserAppService(IElsaAbpEffectivePermissionsProvider effectivePermissionsProvider)
    {
        _effectivePermissionsProvider = effectivePermissionsProvider;
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
            TenantName = CurrentUser.TenantId.HasValue ? CurrentTenant.Name : null,
            Roles = CurrentUser.Roles.ToList(),
            Permissions = permissions.ToList()
        };
    }
}
