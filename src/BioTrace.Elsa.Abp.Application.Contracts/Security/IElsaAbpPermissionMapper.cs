namespace BioTrace.Elsa.Abp.Security;

public interface IElsaAbpPermissionMapper
{
    /// <summary>
    /// Maps granted ABP permission names to Elsa API permission claim values.
    /// </summary>
    IReadOnlyList<string> MapToElsaPermissions(IEnumerable<string> grantedAbpPermissionNames);
}
