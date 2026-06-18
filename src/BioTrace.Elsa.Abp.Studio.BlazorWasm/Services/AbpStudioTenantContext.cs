using BioTrace.Elsa.Abp.Elsa;
using BioTrace.Elsa.Abp.Studio.Models;
using BioTrace.Elsa.Abp.Studio.Services;
using Microsoft.JSInterop;

namespace BioTrace.Elsa.Abp.Studio.Services;

public class AbpStudioTenantContext : IAbpStudioTenantContext
{
    public const string LocalStorageKey = "biotrace.elsa.studio.tenant";

    public const string LocalStorageIdKey = "biotrace.elsa.studio.tenantId";

    private readonly IJSRuntime _jsRuntime;
    private readonly IStudioTenantDirectory _tenantDirectory;
    private Guid? _currentTenantId;
    private string? _currentTenantName;
    private string? _currentTenantDisplayName;
    private bool _isTenantLocked;
    private bool _initialized;
    private Guid? _lastUserId;

    public AbpStudioTenantContext(
        IJSRuntime jsRuntime,
        IStudioTenantDirectory tenantDirectory)
    {
        _jsRuntime = jsRuntime;
        _tenantDirectory = tenantDirectory;
    }

    public Guid? CurrentTenantId => _currentTenantId;

    public string? CurrentTenantName => _currentTenantName;

    public string? CurrentTenantDisplayName => _currentTenantDisplayName;

    public bool IsTenantLocked => _isTenantLocked;

    public event Action? TenantChanged;

    public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        _currentTenantId = await ReadStoredTenantIdAsync(cancellationToken);
        await RefreshTenantMetadataAsync(cancellationToken);
        _initialized = true;
    }

    public virtual async Task InitializeFromUserAsync(
        ElsaAbpCurrentUserDto currentUser,
        CancellationToken cancellationToken = default)
    {
        if (_lastUserId != currentUser.UserId)
        {
            _lastUserId = currentUser.UserId;
            _initialized = false;
            _currentTenantId = null;
            _currentTenantName = null;
            _currentTenantDisplayName = null;
            _isTenantLocked = false;
        }

        await InitializeAsync(cancellationToken);

        if (currentUser.TenantId.HasValue)
        {
            _isTenantLocked = true;
            await ApplyTenantAsync(
                currentUser.TenantId,
                fallbackDisplayName: currentUser.TenantName,
                cancellationToken);
            return;
        }

        _isTenantLocked = false;

        if (!_currentTenantId.HasValue)
        {
            return;
        }

        TenantChanged?.Invoke();
    }

    public virtual async Task SetCurrentTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default)
    {
        if (_isTenantLocked)
        {
            return;
        }

        await ApplyTenantAsync(tenantId, cancellationToken: cancellationToken);
    }

    public virtual async Task ClearTenantAsync(CancellationToken cancellationToken = default)
    {
        _lastUserId = null;
        _initialized = false;
        _isTenantLocked = false;
        await ApplyTenantAsync(null, cancellationToken: cancellationToken);
    }

    protected virtual async Task ApplyTenantAsync(
        Guid? tenantId,
        string? fallbackDisplayName = null,
        CancellationToken cancellationToken = default)
    {
        if (_currentTenantId == tenantId)
        {
            return;
        }

        _currentTenantId = tenantId;
        await WriteStoredTenantIdAsync(tenantId, cancellationToken);

        if (!tenantId.HasValue)
        {
            _currentTenantName = null;
            _currentTenantDisplayName = null;
            TenantChanged?.Invoke();
            return;
        }

        var (name, displayName) = await ResolveTenantInfoAsync(tenantId.Value, cancellationToken);
        _currentTenantName = name;
        _currentTenantDisplayName = displayName
                                    ?? fallbackDisplayName
                                    ?? name
                                    ?? tenantId.Value.ToString("D");
        TenantChanged?.Invoke();
    }

    protected virtual async Task RefreshTenantMetadataAsync(CancellationToken cancellationToken)
    {
        if (!_currentTenantId.HasValue)
        {
            _currentTenantName = null;
            _currentTenantDisplayName = null;
            return;
        }

        var (name, displayName) = await ResolveTenantInfoAsync(_currentTenantId.Value, cancellationToken);
        _currentTenantName = name;
        _currentTenantDisplayName = displayName ?? name ?? _currentTenantId.Value.ToString("D");
    }

    protected virtual async Task<(string? Name, string? DisplayName)> ResolveTenantInfoAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenants = await _tenantDirectory.GetTenantsAsync(cancellationToken);
        var tenant = FindTenantById(tenants, tenantId);
        if (tenant == null)
        {
            return (null, null);
        }

        var displayName = string.IsNullOrWhiteSpace(tenant.DisplayName) ? tenant.Name : tenant.DisplayName;
        return (tenant.Name, displayName);
    }

    protected virtual async Task<Guid?> ResolveTenantIdByNameAsync(
        string tenantName,
        CancellationToken cancellationToken = default)
    {
        var tenants = await _tenantDirectory.GetTenantsAsync(cancellationToken);
        var tenant = tenants.FirstOrDefault(item =>
            string.Equals(item.Name, tenantName, StringComparison.OrdinalIgnoreCase));

        if (tenant == null || !Guid.TryParse(tenant.Id, out var tenantId))
        {
            return null;
        }

        return tenantId;
    }

    protected virtual StudioTenantOption? FindTenantById(
        IReadOnlyList<StudioTenantOption> tenants,
        Guid tenantId)
    {
        return tenants.FirstOrDefault(tenant =>
            string.Equals(tenant.Id, tenantId.ToString("D"), StringComparison.OrdinalIgnoreCase)
            || string.Equals(tenant.Id, tenantId.ToString("N"), StringComparison.OrdinalIgnoreCase));
    }

    protected virtual async Task<Guid?> ReadStoredTenantIdAsync(CancellationToken cancellationToken)
    {
        var storedTenantId = await ReadLocalStorageAsync(LocalStorageIdKey, cancellationToken);
        if (Guid.TryParse(storedTenantId, out var tenantId))
        {
            return tenantId;
        }

        var legacyTenantName = await ReadLocalStorageAsync(LocalStorageKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(legacyTenantName))
        {
            return null;
        }

        var migratedTenantId = await ResolveTenantIdByNameAsync(legacyTenantName, cancellationToken);
        if (!migratedTenantId.HasValue)
        {
            return null;
        }

        await WriteStoredTenantIdAsync(migratedTenantId, cancellationToken);
        await RemoveLocalStorageAsync(LocalStorageKey, cancellationToken);
        return migratedTenantId;
    }

    protected virtual async Task WriteStoredTenantIdAsync(Guid? tenantId, CancellationToken cancellationToken)
    {
        if (!tenantId.HasValue)
        {
            await RemoveLocalStorageAsync(LocalStorageIdKey, cancellationToken);
            await RemoveLocalStorageAsync(LocalStorageKey, cancellationToken);
            return;
        }

        await WriteLocalStorageAsync(LocalStorageIdKey, tenantId.Value.ToString("D"), cancellationToken);
        await RemoveLocalStorageAsync(LocalStorageKey, cancellationToken);
    }

    protected virtual async Task<string?> ReadLocalStorageAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var value = await _jsRuntime.InvokeAsync<string?>(
                "localStorage.getItem",
                cancellationToken,
                key);

            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
        catch (JSException)
        {
            return null;
        }
    }

    protected virtual async Task WriteLocalStorageAsync(
        string key,
        string value,
        CancellationToken cancellationToken)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync(
                "localStorage.setItem",
                cancellationToken,
                key,
                value);
        }
        catch (JSException)
        {
            // Ignore storage failures in restricted browser contexts.
        }
    }

    protected virtual async Task RemoveLocalStorageAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync(
                "localStorage.removeItem",
                cancellationToken,
                key);
        }
        catch (JSException)
        {
            // Ignore storage failures in restricted browser contexts.
        }
    }
}
