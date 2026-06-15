using BioTrace.Elsa.Abp.Permissions;
using Microsoft.Extensions.Options;
using Volo.Abp.Data;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Uow;

namespace BioTrace.Elsa.Abp.Data;

public class ElsaAbpHostDataSeedContributor : ElsaAbpPermissionDataSeedContributor
{
    public ElsaAbpHostDataSeedContributor(
        IGuidGenerator guidGenerator,
        ICurrentTenant currentTenant,
        IIdentityRoleRepository roleRepository,
        IIdentityUserRepository userRepository,
        IdentityUserManager userManager,
        IPermissionManager permissionManager,
        Microsoft.AspNetCore.Identity.ILookupNormalizer lookupNormalizer,
        IUnitOfWorkManager unitOfWorkManager,
        IOptions<ElsaAbpPermissionSeedOptions> permissionSeedOptions)
        : base(
            guidGenerator,
            currentTenant,
            roleRepository,
            userRepository,
            userManager,
            permissionManager,
            lookupNormalizer,
            unitOfWorkManager,
            permissionSeedOptions)
    {
    }

    public override async Task SeedAsync(DataSeedContext context)
    {
        using var uow = UnitOfWorkManager.Begin(requiresNew: true, isTransactional: true);

        var adminRole = await SeedRoleAsync("admin", "Administrator", isDefault: true);

        await SeedUserAsync("admin", "admin@localhost", "1q2w3E*", adminRole.Name);

        await GrantRolePermissionsAsync(adminRole.Name,
            AbpElsaPermissions.Admin,
            AbpElsaPermissions.NotReadOnly,
            "AbpTenantManagement.Tenants");

        await uow.CompleteAsync();
    }
}
