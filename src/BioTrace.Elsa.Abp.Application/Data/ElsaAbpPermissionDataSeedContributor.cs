using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Uow;

namespace BioTrace.Elsa.Abp.Data;

/// <summary>
/// Base data seed contributor for granting configured <c>Abp.Elsa.*</c> permissions to ABP roles.
/// </summary>
public abstract class ElsaAbpPermissionDataSeedContributor : IDataSeedContributor, IElsaAbpPermissionSeeder, ITransientDependency
{
    protected IGuidGenerator GuidGenerator { get; }
    protected ICurrentTenant CurrentTenant { get; }
    protected IIdentityRoleRepository RoleRepository { get; }
    protected IIdentityUserRepository UserRepository { get; }
    protected IdentityUserManager UserManager { get; }
    protected IPermissionManager PermissionManager { get; }
    protected ILookupNormalizer LookupNormalizer { get; }
    protected IUnitOfWorkManager UnitOfWorkManager { get; }
    protected ElsaAbpPermissionSeedOptions PermissionSeedOptions { get; }

    protected ElsaAbpPermissionDataSeedContributor(
        IGuidGenerator guidGenerator,
        ICurrentTenant currentTenant,
        IIdentityRoleRepository roleRepository,
        IIdentityUserRepository userRepository,
        IdentityUserManager userManager,
        IPermissionManager permissionManager,
        ILookupNormalizer lookupNormalizer,
        IUnitOfWorkManager unitOfWorkManager,
        IOptions<ElsaAbpPermissionSeedOptions> permissionSeedOptions)
    {
        GuidGenerator = guidGenerator;
        CurrentTenant = currentTenant;
        RoleRepository = roleRepository;
        UserRepository = userRepository;
        UserManager = userManager;
        PermissionManager = permissionManager;
        LookupNormalizer = lookupNormalizer;
        UnitOfWorkManager = unitOfWorkManager;
        PermissionSeedOptions = permissionSeedOptions.Value;
    }

    public abstract Task SeedAsync(DataSeedContext context);

    public virtual async Task GrantConfiguredRolePermissionsAsync()
    {
        foreach (var (roleName, permissions) in PermissionSeedOptions.RolePermissions)
        {
            await GrantRolePermissionsAsync(roleName, permissions);
        }
    }

    protected virtual async Task<IdentityRole> SeedRoleAsync(
        string name,
        string? displayName = null,
        bool isDefault = false,
        bool isPublic = true)
    {
        var normalized = LookupNormalizer.NormalizeName(name);
        var role = await RoleRepository.FindByNormalizedNameAsync(normalized);
        if (role != null)
        {
            return role;
        }

        role = CurrentTenant.Id.HasValue
            ? new IdentityRole(GuidGenerator.Create(), name, CurrentTenant.Id)
            : new IdentityRole(GuidGenerator.Create(), name)
            {
                IsDefault = isDefault,
                IsPublic = isPublic
            };

        if (CurrentTenant.Id.HasValue)
        {
            role.IsPublic = isPublic;
        }

        return await RoleRepository.InsertAsync(role, autoSave: true);
    }

    protected virtual async Task SeedUserAsync(string userName, string email, string password, string roleName)
    {
        if (await UserRepository.FindByNormalizedUserNameAsync(LookupNormalizer.NormalizeName(userName)) != null)
        {
            return;
        }

        var user = CurrentTenant.Id.HasValue
            ? new IdentityUser(GuidGenerator.Create(), userName, email, CurrentTenant.Id)
            : new IdentityUser(GuidGenerator.Create(), userName, email);

        await UserManager.CreateAsync(user, password);
        await UserManager.AddToRoleAsync(user, roleName);
    }

    protected virtual async Task GrantRolePermissionsAsync(string roleName, params string[] permissions)
    {
        foreach (var permission in permissions)
        {
            await PermissionManager.SetForRoleAsync(roleName, permission, true);
        }
    }
}
