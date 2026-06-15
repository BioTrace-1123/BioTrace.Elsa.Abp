namespace BioTrace.Elsa.Abp.Data;

/// <summary>
/// Maps ABP role names to <c>Abp.Elsa.*</c> permissions for <see cref="ElsaAbpPermissionDataSeedContributor"/>.
/// </summary>
public class ElsaAbpPermissionSeedOptions
{
    /// <summary>
    /// Role name to granted Elsa permission names (e.g. <c>admin</c> → <c>Abp.Elsa.Admin</c>).
    /// </summary>
    public Dictionary<string, string[]> RolePermissions { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
