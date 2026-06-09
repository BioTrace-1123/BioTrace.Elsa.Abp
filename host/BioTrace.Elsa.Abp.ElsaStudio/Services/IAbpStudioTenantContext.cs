using BioTrace.Elsa.Abp.ElsaStudio.Models;

namespace BioTrace.Elsa.Abp.ElsaStudio.Services;

public interface IAbpStudioTenantContext
{
    string? CurrentTenantName { get; }

    bool IsTenantLocked { get; }

    event Action? TenantChanged;

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task InitializeFromUserAsync(ElsaAbpCurrentUserResponse currentUser, CancellationToken cancellationToken = default);

    Task SetCurrentTenantAsync(string? tenantName, CancellationToken cancellationToken = default);

    Task ClearTenantAsync(CancellationToken cancellationToken = default);
}
