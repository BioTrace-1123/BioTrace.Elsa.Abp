using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BioTrace.Elsa.Abp.Studio.Models;
using Elsa.Studio.Authentication.OpenIdConnect.Contracts;
using Microsoft.Extensions.Options;

namespace BioTrace.Elsa.Abp.Studio.Services;

public class AbpApiStudioTenantDirectory : IStudioTenantDirectory
{
    private readonly HttpClient _httpClient;
    private readonly BioTraceElsaAbpStudioOptions _options;
    private readonly ITokenProvider _tokenProvider;

    public AbpApiStudioTenantDirectory(
        HttpClient httpClient,
        IOptions<BioTraceElsaAbpStudioOptions> options,
        ITokenProvider tokenProvider)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _tokenProvider = tokenProvider;
    }

    public virtual async Task<IReadOnlyList<StudioTenantOption>> GetTenantsAsync(CancellationToken cancellationToken = default)
    {
        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            return [];
        }

        var path = string.IsNullOrWhiteSpace(_options.Tenancy.TenantsApiPath)
            ? "api/multi-tenancy/tenants?MaxResultCount=1000"
            : _options.Tenancy.TenantsApiPath.TrimStart('/');

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var payload = await response.Content.ReadFromJsonAsync<AbpPagedTenantResponse>(cancellationToken);
        if (payload?.Items == null || payload.Items.Count == 0)
        {
            return [];
        }

        return payload.Items
            .Where(tenant => !string.IsNullOrWhiteSpace(tenant.Name))
            .Select(tenant => new StudioTenantOption
            {
                Id = tenant.Id.ToString("D"),
                Name = tenant.Name!,
                DisplayName = string.IsNullOrWhiteSpace(tenant.Name) ? tenant.Name! : tenant.Name!
            })
            .ToList();
    }

    private sealed class AbpPagedTenantResponse
    {
        [JsonPropertyName("items")]
        public List<AbpTenantListItem>? Items { get; set; }
    }

    private sealed class AbpTenantListItem
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
