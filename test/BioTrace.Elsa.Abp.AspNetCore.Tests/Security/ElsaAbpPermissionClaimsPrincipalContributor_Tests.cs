using System.Security.Claims;
using BioTrace.Elsa.Abp.Permissions;
using BioTrace.Elsa.Abp.Security;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Volo.Abp.Security.Claims;
using Xunit;

namespace BioTrace.Elsa.Abp.Security;

public class ElsaAbpPermissionClaimsPrincipalContributor_Tests
{
    [Fact]
    public async Task Should_inject_permissions_claims_from_effective_permissions_provider()
    {
        var effectivePermissionsProvider = Substitute.For<IElsaAbpEffectivePermissionsProvider>();
        effectivePermissionsProvider
            .GetElsaPermissionsAsync(Arg.Any<ClaimsPrincipal?>(), Arg.Any<CancellationToken>())
            .Returns([ElsaApiPermissionNames.WorkflowDefinitions.Read]);

        var contributor = new ElsaAbpPermissionClaimsPrincipalContributor(
            effectivePermissionsProvider,
            Options.Create(new ElsaAbpOptions { EnablePermissionClaimsBridge = true }));

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "Bearer");
        var principal = new ClaimsPrincipal(identity);
        var context = new AbpClaimsPrincipalContributorContext(principal, null!);

        await contributor.ContributeAsync(context);

        identity.HasClaim(c => c.Type == "permissions" && c.Value == ElsaApiPermissionNames.WorkflowDefinitions.Read)
            .ShouldBeTrue();
    }

    [Fact]
    public async Task Should_not_inject_permissions_when_bridge_disabled()
    {
        var effectivePermissionsProvider = Substitute.For<IElsaAbpEffectivePermissionsProvider>();
        effectivePermissionsProvider
            .GetElsaPermissionsAsync(Arg.Any<ClaimsPrincipal?>(), Arg.Any<CancellationToken>())
            .Returns([ElsaApiPermissionNames.Wildcard]);

        var contributor = new ElsaAbpPermissionClaimsPrincipalContributor(
            effectivePermissionsProvider,
            Options.Create(new ElsaAbpOptions { EnablePermissionClaimsBridge = false }));

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "Bearer");
        var principal = new ClaimsPrincipal(identity);
        var context = new AbpClaimsPrincipalContributorContext(principal, null!);

        await contributor.ContributeAsync(context);

        identity.HasClaim(c => c.Type == "permissions").ShouldBeFalse();
        await effectivePermissionsProvider.DidNotReceiveWithAnyArgs()
            .GetElsaPermissionsAsync(default!, default);
    }
}
