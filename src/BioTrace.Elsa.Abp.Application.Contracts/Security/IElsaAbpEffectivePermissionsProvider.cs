namespace BioTrace.Elsa.Abp.Security;

public interface IElsaAbpEffectivePermissionsProvider
{
    /// <summary>
    /// Resolves Elsa API permission strings for the current (or supplied) principal.
    /// Results are memoized per HTTP request when <see cref="Microsoft.AspNetCore.Http.HttpContext"/> is available.
    /// </summary>
    Task<IReadOnlyList<string>> GetElsaPermissionsAsync(
        System.Security.Claims.ClaimsPrincipal? principal = null,
        CancellationToken cancellationToken = default);
}
