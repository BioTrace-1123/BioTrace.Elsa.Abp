using BioTrace.Elsa.Abp.Permissions;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Uow;

namespace BioTrace.Elsa.Abp.Data;

public class ElsaAbpHostDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IGuidGenerator _guidGenerator;
    private readonly IIdentityRoleRepository _roleRepository;
    private readonly IIdentityUserRepository _userRepository;
    private readonly IdentityUserManager _userManager;
    private readonly IPermissionManager _permissionManager;
    private readonly Microsoft.AspNetCore.Identity.ILookupNormalizer _lookupNormalizer;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public ElsaAbpHostDataSeedContributor(
        IGuidGenerator guidGenerator,
        IIdentityRoleRepository roleRepository,
        IIdentityUserRepository userRepository,
        IdentityUserManager userManager,
        IPermissionManager permissionManager,
        Microsoft.AspNetCore.Identity.ILookupNormalizer lookupNormalizer,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _guidGenerator = guidGenerator;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _userManager = userManager;
        _permissionManager = permissionManager;
        _lookupNormalizer = lookupNormalizer;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public virtual async Task SeedAsync(DataSeedContext context)
    {
        using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);

        var adminRole = await SeedRoleAsync("admin", "Administrator");
        var designerRole = await SeedRoleAsync("designer", "Workflow Designer");
        var operatorRole = await SeedRoleAsync("operator", "Workflow Operator");

        await SeedUserAsync("admin", "admin@localhost", "1q2w3E*", adminRole.Name);
        await SeedUserAsync("designer", "designer@localhost", "1q2w3E*", designerRole.Name);
        await SeedUserAsync("operator", "operator@localhost", "1q2w3E*", operatorRole.Name);

        await GrantRolePermissionsAsync(adminRole.Name,
            AbpElsaPermissions.Admin,
            AbpElsaPermissions.NotReadOnly);
        await GrantRolePermissionsAsync(designerRole.Name,
            AbpElsaPermissions.WorkflowDefinitions.Read,
            AbpElsaPermissions.WorkflowInstances.Read);
        await GrantRolePermissionsAsync(operatorRole.Name,
            AbpElsaPermissions.WorkflowDefinitions.Read,
            AbpElsaPermissions.WorkflowInstances.Read,
            AbpElsaPermissions.WorkflowInstances.Execute,
            AbpElsaPermissions.WorkflowInstances.Cancel);

        await uow.CompleteAsync();
    }

    protected virtual async Task<Volo.Abp.Identity.IdentityRole> SeedRoleAsync(string name, string displayName)
    {
        var normalized = _lookupNormalizer.NormalizeName(name);
        var role = await _roleRepository.FindByNormalizedNameAsync(normalized);
        if (role != null)
        {
            return role;
        }

        role = new Volo.Abp.Identity.IdentityRole(_guidGenerator.Create(), name)
        {
            IsDefault = name == "admin",
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
            email);

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
