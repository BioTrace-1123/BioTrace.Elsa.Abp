using BioTrace.Elsa.Abp.MultiTenancy;
using BioTrace.Elsa.Abp.Permissions;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement;
using Volo.Abp.TenantManagement;
using Volo.Abp.Uow;

namespace BioTrace.Elsa.Abp.Data;

public class ElsaAbpMultiTenancyHostDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IGuidGenerator _guidGenerator;
    private readonly ICurrentTenant _currentTenant;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantManager _tenantManager;
    private readonly IIdentityRoleRepository _roleRepository;
    private readonly IIdentityUserRepository _userRepository;
    private readonly IdentityUserManager _userManager;
    private readonly IPermissionManager _permissionManager;
    private readonly Microsoft.AspNetCore.Identity.ILookupNormalizer _lookupNormalizer;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public ElsaAbpMultiTenancyHostDataSeedContributor(
        IGuidGenerator guidGenerator,
        ICurrentTenant currentTenant,
        ITenantRepository tenantRepository,
        ITenantManager tenantManager,
        IIdentityRoleRepository roleRepository,
        IIdentityUserRepository userRepository,
        IdentityUserManager userManager,
        IPermissionManager permissionManager,
        Microsoft.AspNetCore.Identity.ILookupNormalizer lookupNormalizer,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _guidGenerator = guidGenerator;
        _currentTenant = currentTenant;
        _tenantRepository = tenantRepository;
        _tenantManager = tenantManager;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _userManager = userManager;
        _permissionManager = permissionManager;
        _lookupNormalizer = lookupNormalizer;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public virtual async Task SeedAsync(DataSeedContext context)
    {
        await SeedTenantAsync(
            ElsaAbpMultiTenancySeedData.TenantAName,
            "tenant-a-admin@localhost",
            ElsaAbpMultiTenancySeedData.TenantAAdminUserName,
            designerUserName: ElsaAbpMultiTenancySeedData.TenantADesignerUserName,
            designerEmail: "tenant-a-designer@localhost");

        await SeedTenantAsync(
            ElsaAbpMultiTenancySeedData.TenantBName,
            "tenant-b-admin@localhost",
            ElsaAbpMultiTenancySeedData.TenantBAdminUserName,
            designerUserName: ElsaAbpMultiTenancySeedData.TenantBDesignerUserName,
            designerEmail: "tenant-b-designer@localhost");
    }

    protected virtual async Task SeedTenantAsync(
        string tenantName,
        string adminEmail,
        string adminUserName,
        string designerUserName,
        string designerEmail)
    {
        var tenantId = await EnsureTenantIdAsync(tenantName);
        await SeedTenantUsersAsync(tenantId, adminUserName, adminEmail, designerUserName, designerEmail);
    }

    protected virtual async Task<Guid> EnsureTenantIdAsync(string tenantName)
    {
        var tenant = await _tenantRepository.FindByNameAsync(tenantName);
        if (tenant != null)
        {
            return tenant.Id;
        }

        using (var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true))
        {
            tenant = await _tenantManager.CreateAsync(tenantName);
            await _tenantRepository.InsertAsync(tenant, autoSave: true);
            await uow.CompleteAsync();
            return tenant.Id;
        }
    }

    protected virtual async Task SeedTenantUsersAsync(
        Guid tenantId,
        string adminUserName,
        string adminEmail,
        string designerUserName,
        string designerEmail)
    {
        using (_currentTenant.Change(tenantId))
        {
            var adminRole = await SeedRoleAsync("admin", "Administrator");
            var designerRole = await SeedRoleAsync("designer", "Workflow Designer");

            await SeedUserAsync(adminUserName, adminEmail, "1q2w3E*", adminRole.Name);
            await SeedUserAsync(designerUserName, designerEmail, "1q2w3E*", designerRole.Name);

            await GrantRolePermissionsAsync(adminRole.Name,
                AbpElsaPermissions.NotReadOnly,
                AbpElsaPermissions.WorkflowDefinitions.Read,
                AbpElsaPermissions.WorkflowDefinitions.Write,
                AbpElsaPermissions.WorkflowDefinitions.Publish,
                AbpElsaPermissions.WorkflowDefinitions.Delete,
                AbpElsaPermissions.WorkflowInstances.Read,
                AbpElsaPermissions.WorkflowInstances.Write,
                AbpElsaPermissions.WorkflowInstances.Execute,
                AbpElsaPermissions.WorkflowInstances.Cancel,
                AbpElsaPermissions.WorkflowInstances.Delete);

            await GrantRolePermissionsAsync(designerRole.Name,
                AbpElsaPermissions.WorkflowDefinitions.Read,
                AbpElsaPermissions.WorkflowInstances.Read);
        }
    }

    protected virtual async Task<Volo.Abp.Identity.IdentityRole> SeedRoleAsync(string name, string displayName)
    {
        var normalized = _lookupNormalizer.NormalizeName(name);
        var role = await _roleRepository.FindByNormalizedNameAsync(normalized);
        if (role != null)
        {
            return role;
        }

        role = new Volo.Abp.Identity.IdentityRole(
            _guidGenerator.Create(),
            name,
            _currentTenant.Id)
        {
            IsPublic = true
        };
        return await _roleRepository.InsertAsync(role, autoSave: true);
    }

    protected virtual async Task SeedUserAsync(string userName, string email, string password, string roleName)
    {
        if (await _userRepository.FindByNormalizedUserNameAsync(_lookupNormalizer.NormalizeName(userName)) != null)
        {
            return;
        }

        var user = new Volo.Abp.Identity.IdentityUser(
            _guidGenerator.Create(),
            userName,
            email,
            _currentTenant.Id);

        await _userManager.CreateAsync(user, password);
        await _userManager.AddToRoleAsync(user, roleName);
    }

    protected virtual async Task GrantRolePermissionsAsync(string roleName, params string[] permissions)
    {
        foreach (var permission in permissions)
        {
            await _permissionManager.SetForRoleAsync(roleName, permission, true);
        }
    }
}
