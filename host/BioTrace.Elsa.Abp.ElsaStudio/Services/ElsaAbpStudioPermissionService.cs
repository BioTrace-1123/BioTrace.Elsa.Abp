using System.Net.Http.Headers;
using System.Net.Http.Json;
using BioTrace.Elsa.Abp.ElsaStudio.Models;
using Elsa.Studio.Authentication.OpenIdConnect.Contracts;

namespace BioTrace.Elsa.Abp.ElsaStudio.Services;

public class ElsaAbpStudioPermissionService : IElsaAbpStudioPermissionService, IDisposable
{
    public const string WriteWorkflowDefinitionsPermission = "write:workflow-definitions";
    public const string WildcardPermission = "*";

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ITokenProvider _tokenProvider;
    private readonly IAbpStudioTenantContext _tenantContext;
    private IReadOnlyList<string>? _cachedPermissions;

    public ElsaAbpStudioPermissionService(
        HttpClient httpClient,
        IConfiguration configuration,
        ITokenProvider tokenProvider,
        IAbpStudioTenantContext tenantContext)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _tokenProvider = tokenProvider;
        _tenantContext = tenantContext;
        _tenantContext.TenantChanged += OnTenantChanged;
    }

    public virtual async Task<IReadOnlyList<string>> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedPermissions != null)
        {
            return _cachedPermissions;
        }

        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            _cachedPermissions = [];
            return _cachedPermissions;
        }

        await _tenantContext.InitializeAsync(cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, ResolveCurrentUserPath());
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _cachedPermissions = [];
            return _cachedPermissions;
        }

        var currentUser = await response.Content.ReadFromJsonAsync<ElsaAbpCurrentUserResponse>(cancellationToken);
        if (currentUser != null)
        {
            await _tenantContext.InitializeFromUserAsync(currentUser, cancellationToken);
        }

        _cachedPermissions = currentUser?.Permissions ?? [];
        return _cachedPermissions;
    }

    public virtual async Task<bool> CanWriteWorkflowDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await GetPermissionsAsync(cancellationToken);

        return permissions.Contains(WildcardPermission, StringComparer.OrdinalIgnoreCase)
               || permissions.Contains(WriteWorkflowDefinitionsPermission, StringComparer.OrdinalIgnoreCase);
    }

    public virtual void InvalidateCache()
    {
        _cachedPermissions = null;
    }

    public void Dispose()
    {
        _tenantContext.TenantChanged -= OnTenantChanged;
    }

    protected virtual string ResolveCurrentUserPath()
    {
        var configuredPath = _configuration["AbpApi:CurrentUserPath"];
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return configuredPath.TrimStart('/');
        }

        return "identity/users/me";
    }

    private void OnTenantChanged()
    {
        InvalidateCache();
    }
}
