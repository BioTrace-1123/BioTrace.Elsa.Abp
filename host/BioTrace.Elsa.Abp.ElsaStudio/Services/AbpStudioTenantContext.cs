using BioTrace.Elsa.Abp.ElsaStudio.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;

namespace BioTrace.Elsa.Abp.ElsaStudio.Services;

public class AbpStudioTenantContext : IAbpStudioTenantContext
{
    public const string LocalStorageKey = "biotrace.elsa.studio.tenant";

    private readonly IJSRuntime _jsRuntime;
    private readonly IConfiguration _configuration;
    private string? _currentTenantName;
    private bool _isTenantLocked;
    private bool _initialized;
    private Guid? _lastUserId;

    public AbpStudioTenantContext(IJSRuntime jsRuntime, IConfiguration configuration)
    {
        _jsRuntime = jsRuntime;
        _configuration = configuration;
    }

    public string? CurrentTenantName => _currentTenantName;

    public bool IsTenantLocked => _isTenantLocked;

    public event Action? TenantChanged;

    public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        _currentTenantName = await ReadStoredTenantNameAsync(cancellationToken);
        _initialized = true;
    }

    public virtual async Task InitializeFromUserAsync(
        ElsaAbpCurrentUserResponse currentUser,
        CancellationToken cancellationToken = default)
    {
        if (_lastUserId != currentUser.UserId)
        {
            _lastUserId = currentUser.UserId;
            _initialized = false;
            _currentTenantName = null;
            _isTenantLocked = false;
        }

        await InitializeAsync(cancellationToken);

        if (currentUser.TenantId.HasValue)
        {
            _isTenantLocked = true;
            var tenantKey = await ResolveTenantKeyAsync(currentUser, cancellationToken);
            if (!string.IsNullOrWhiteSpace(tenantKey))
            {
                await ApplyTenantAsync(tenantKey, cancellationToken);
            }

            return;
        }

        _isTenantLocked = false;

        if (string.IsNullOrWhiteSpace(_currentTenantName))
        {
            return;
        }

        TenantChanged?.Invoke();
    }

    public virtual async Task SetCurrentTenantAsync(string? tenantName, CancellationToken cancellationToken = default)
    {
        if (_isTenantLocked)
        {
            return;
        }

        await ApplyTenantAsync(tenantName, cancellationToken);
    }

    public virtual async Task ClearTenantAsync(CancellationToken cancellationToken = default)
    {
        _lastUserId = null;
        _initialized = false;
        _isTenantLocked = false;
        await ApplyTenantAsync(null, cancellationToken);
    }

    protected virtual async Task<string?> ResolveTenantKeyAsync(
        ElsaAbpCurrentUserResponse currentUser,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(currentUser.TenantName))
        {
            return currentUser.TenantName.Trim();
        }

        if (currentUser.TenantId.HasValue)
        {
            var configuredName = ResolveConfiguredTenantName(currentUser.TenantId.Value);
            if (!string.IsNullOrWhiteSpace(configuredName))
            {
                return configuredName;
            }

            return currentUser.TenantId.Value.ToString("D");
        }

        return null;
    }

    protected virtual string? ResolveConfiguredTenantName(Guid tenantId)
    {
        var tenants = _configuration.GetSection("Tenancy:Tenants").Get<List<StudioTenantOption>>() ?? [];
        return tenants.FirstOrDefault(tenant =>
                string.Equals(tenant.Id, tenantId.ToString("D"), StringComparison.OrdinalIgnoreCase)
                || string.Equals(tenant.Id, tenantId.ToString("N"), StringComparison.OrdinalIgnoreCase))
            ?.Name;
    }

    protected virtual async Task ApplyTenantAsync(string? tenantName, CancellationToken cancellationToken)
    {
        var normalizedTenantName = string.IsNullOrWhiteSpace(tenantName) ? null : tenantName.Trim();
        if (string.Equals(_currentTenantName, normalizedTenantName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _currentTenantName = normalizedTenantName;
        await WriteStoredTenantNameAsync(normalizedTenantName, cancellationToken);
        TenantChanged?.Invoke();
    }

    protected virtual async Task<string?> ReadStoredTenantNameAsync(CancellationToken cancellationToken)
    {
        try
        {
            var storedTenantName = await _jsRuntime.InvokeAsync<string?>(
                "localStorage.getItem",
                cancellationToken,
                LocalStorageKey);

            return string.IsNullOrWhiteSpace(storedTenantName) ? null : storedTenantName.Trim();
        }
        catch (JSException)
        {
            return null;
        }
    }

    protected virtual async Task WriteStoredTenantNameAsync(string? tenantName, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tenantName))
            {
                await _jsRuntime.InvokeVoidAsync(
                    "localStorage.removeItem",
                    cancellationToken,
                    LocalStorageKey);
                return;
            }

            await _jsRuntime.InvokeVoidAsync(
                "localStorage.setItem",
                cancellationToken,
                LocalStorageKey,
                tenantName);
        }
        catch (JSException)
        {
            // Ignore storage failures in restricted browser contexts.
        }
    }
}
