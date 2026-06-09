using BioTrace.Elsa.Abp.ElsaStudio.Models;
using Microsoft.JSInterop;

namespace BioTrace.Elsa.Abp.ElsaStudio.Services;

public class AbpStudioTenantContext : IAbpStudioTenantContext
{
    public const string LocalStorageKey = "biotrace.elsa.studio.tenant";

    private readonly IJSRuntime _jsRuntime;
    private string? _currentTenantName;
    private bool _isTenantLocked;
    private bool _initialized;

    public AbpStudioTenantContext(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
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
        await InitializeAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(currentUser.TenantName))
        {
            _isTenantLocked = true;
            await ApplyTenantAsync(currentUser.TenantName, cancellationToken);
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
