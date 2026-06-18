using BioTrace.Elsa.Abp.Elsa;

namespace BioTrace.Elsa.Abp.Studio.Services;

public interface IAbpStudioTenantContext
{
    Guid? CurrentTenantId { get; }

    /// <summary>ABP tenant name (internal identifier), not sent as HTTP header.</summary>
    string? CurrentTenantName { get; }

    /// <summary>Human-readable tenant label for UI (e.g. localized display name).</summary>
    string? CurrentTenantDisplayName { get; }

    bool IsTenantLocked { get; }

    event Action? TenantChanged;

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task InitializeFromUserAsync(ElsaAbpCurrentUserDto currentUser, CancellationToken cancellationToken = default);

    Task SetCurrentTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    Task ClearTenantAsync(CancellationToken cancellationToken = default);
}
