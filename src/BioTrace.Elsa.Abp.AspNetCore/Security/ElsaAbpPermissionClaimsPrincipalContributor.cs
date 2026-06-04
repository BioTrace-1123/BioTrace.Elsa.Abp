using System.Security.Claims;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Security.Claims;

namespace BioTrace.Elsa.Abp.Security;

public class ElsaAbpPermissionClaimsPrincipalContributor : IAbpClaimsPrincipalContributor, ITransientDependency
{
    private readonly IElsaAbpEffectivePermissionsProvider _effectivePermissionsProvider;
    private readonly IOptions<ElsaAbpOptions> _options;

    public ElsaAbpPermissionClaimsPrincipalContributor(
        IElsaAbpEffectivePermissionsProvider effectivePermissionsProvider,
        IOptions<ElsaAbpOptions> options)
    {
        _effectivePermissionsProvider = effectivePermissionsProvider;
        _options = options;
    }

    public virtual async Task ContributeAsync(AbpClaimsPrincipalContributorContext context)
    {
        if (!_options.Value.EnablePermissionClaimsBridge)
        {
            return;
        }

        var identity = context.ClaimsPrincipal.Identity as ClaimsIdentity;
        if (identity == null || !context.ClaimsPrincipal.Identity!.IsAuthenticated)
        {
            return;
        }

        var claimType = _options.Value.PermissionsClaimType;
        RemoveExistingPermissionClaims(identity, claimType);

        var elsaPermissions = await _effectivePermissionsProvider.GetElsaPermissionsAsync(
            context.ClaimsPrincipal);

        foreach (var permission in elsaPermissions)
        {
            identity.AddClaim(new System.Security.Claims.Claim(claimType, permission));
        }
    }

    protected virtual void RemoveExistingPermissionClaims(ClaimsIdentity identity, string claimType)
    {
        var existing = identity.FindAll(claimType).ToList();
        foreach (var claim in existing)
        {
            identity.RemoveClaim(claim);
        }
    }
}
