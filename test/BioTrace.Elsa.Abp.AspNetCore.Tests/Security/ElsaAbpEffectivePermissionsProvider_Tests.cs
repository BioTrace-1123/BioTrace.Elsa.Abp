using System.Security.Claims;
using BioTrace.Elsa.Abp.Permissions;
using BioTrace.Elsa.Abp.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Users;
using Xunit;

namespace BioTrace.Elsa.Abp.Security;

public class ElsaAbpEffectivePermissionsProvider_Tests
{
    [Fact]
    public async Task Admin_should_short_circuit_without_permission_manager_calls()
    {
        var permissionManager = Substitute.For<IPermissionManager>();
        var permissionChecker = Substitute.For<IPermissionChecker>();
        permissionChecker.IsGrantedAsync(AbpElsaPermissions.Admin).Returns(true);

        var provider = CreateProvider(permissionManager, permissionChecker);
        var result = await provider.GetElsaPermissionsAsync();

        result.ShouldBe([ElsaApiPermissionNames.Wildcard]);
        await permissionManager.DidNotReceiveWithAnyArgs().GetAllAsync(default!, default!);
        await permissionChecker.Received(1).IsGrantedAsync(AbpElsaPermissions.Admin);
    }

    [Fact]
    public async Task Host_admin_should_resolve_wildcard_when_current_tenant_is_switched()
    {
        var permissionManager = Substitute.For<IPermissionManager>();
        var permissionChecker = Substitute.For<IPermissionChecker>();
        var currentTenant = CreateCurrentTenant(Guid.NewGuid());
        permissionChecker
            .IsGrantedAsync(AbpElsaPermissions.Admin)
            .Returns(_ => Task.FromResult(currentTenant.Id == null));
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.TenantId.Returns((Guid?)null);
        currentUser.Id.Returns(Guid.NewGuid());
        currentUser.Roles.Returns([]);

        var provider = new ElsaAbpEffectivePermissionsProvider(
            permissionManager,
            permissionChecker,
            new ElsaAbpPermissionMapper(),
            currentUser,
            currentTenant,
            Substitute.For<IHttpContextAccessor>(),
            Options.Create(new ElsaAbpOptions()));

        var result = await provider.GetElsaPermissionsAsync();
        result.ShouldBe([ElsaApiPermissionNames.Wildcard]);

        await permissionChecker.Received(1).IsGrantedAsync(AbpElsaPermissions.Admin);
        await permissionManager.DidNotReceiveWithAnyArgs().GetAllAsync(default!, default!);
    }

    [Fact]
    public async Task Should_cache_permissions_in_http_context_items()
    {
        var permissionManager = Substitute.For<IPermissionManager>();
        var permissionChecker = Substitute.For<IPermissionChecker>();
        permissionChecker.IsGrantedAsync(AbpElsaPermissions.Admin).Returns(false);
        permissionManager
            .GetAllAsync(UserPermissionValueProvider.ProviderName, Arg.Any<string>())
            .Returns([
                new PermissionWithGrantedProviders(AbpElsaPermissions.WorkflowDefinitions.Read, true)
            ]);

        var httpContext = new DefaultHttpContext();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.Id.Returns(Guid.NewGuid());
        currentUser.Roles.Returns([]);

        var provider = new ElsaAbpEffectivePermissionsProvider(
            permissionManager,
            permissionChecker,
            new ElsaAbpPermissionMapper(),
            currentUser,
            CreateCurrentTenant(),
            httpContextAccessor,
            Options.Create(new ElsaAbpOptions()));

        var first = await provider.GetElsaPermissionsAsync();
        var second = await provider.GetElsaPermissionsAsync();

        first.ShouldBe(second);
        await permissionManager.Received(1)
            .GetAllAsync(UserPermissionValueProvider.ProviderName, Arg.Any<string>());
    }

    private static ElsaAbpEffectivePermissionsProvider CreateProvider(
        IPermissionManager permissionManager,
        IPermissionChecker permissionChecker)
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.Id.Returns(Guid.NewGuid());
        currentUser.Roles.Returns([]);

        return new ElsaAbpEffectivePermissionsProvider(
            permissionManager,
            permissionChecker,
            new ElsaAbpPermissionMapper(),
            currentUser,
            CreateCurrentTenant(),
            Substitute.For<IHttpContextAccessor>(),
            Options.Create(new ElsaAbpOptions()));
    }

    private static ICurrentTenant CreateCurrentTenant(Guid? tenantId = null)
    {
        var activeTenantId = tenantId;
        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Id.Returns(_ => activeTenantId);
        currentTenant.Change(Arg.Any<Guid?>()).Returns(callInfo =>
        {
            var previous = activeTenantId;
            activeTenantId = callInfo.Arg<Guid?>();
            return new TenantScopeRestore(() => activeTenantId = previous);
        });
        return currentTenant;
    }

    private sealed class TenantScopeRestore(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }
}
