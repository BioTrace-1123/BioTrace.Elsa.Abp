using BioTrace.Elsa.Abp.Elsa;

namespace BioTrace.Elsa.Abp.Studio.Services;

public interface IAbpStudioTenantContext
{
    string? CurrentTenantName { get; }

    bool IsTenantLocked { get; }

    event Action? TenantChanged;

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task InitializeFromUserAsync(ElsaAbpCurrentUserDto currentUser, CancellationToken cancellationToken = default);

    Task SetCurrentTenantAsync(string? tenantName, CancellationToken cancellationToken = default);

    Task ClearTenantAsync(CancellationToken cancellationToken = default);
}
