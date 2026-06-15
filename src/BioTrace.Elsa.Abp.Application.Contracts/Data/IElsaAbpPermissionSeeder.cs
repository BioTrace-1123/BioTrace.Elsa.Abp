namespace BioTrace.Elsa.Abp.Data;

/// <summary>
/// Grants configured <c>Abp.Elsa.*</c> permissions to ABP roles during data seeding.
/// </summary>
public interface IElsaAbpPermissionSeeder
{
    Task GrantConfiguredRolePermissionsAsync();
}
