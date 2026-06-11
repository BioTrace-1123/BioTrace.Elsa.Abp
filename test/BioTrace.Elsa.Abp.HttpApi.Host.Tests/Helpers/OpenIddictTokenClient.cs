using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace BioTrace.Elsa.Abp.Helpers;

public class OpenIddictTokenClient
{
    public const string ClientId = "BioTrace_Elsa_Abp_IntegrationTests";
    public const string ClientSecret = "integration-test-secret";
    public const string DefaultPassword = "1q2w3E*";

    private readonly HttpClient _httpClient;

    public OpenIddictTokenClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public virtual async Task<string> RequestPasswordTokenAsync(
        string username,
        string? tenantName = null,
        string password = DefaultPassword,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/connect/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = ClientId,
                ["client_secret"] = ClientSecret,
                ["username"] = username,
                ["password"] = password,
                ["scope"] = "BioTrace_Elsa_Abp openid profile roles"
            })
        };

        if (!string.IsNullOrWhiteSpace(tenantName))
        {
            request.Headers.TryAddWithoutValidation("__tenant", tenantName);
        }

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Token request failed ({(int)response.StatusCode}): {body}");
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
        if (string.IsNullOrWhiteSpace(tokenResponse?.AccessToken))
        {
            throw new InvalidOperationException("Token response did not contain access_token.");
        }

        return tokenResponse.AccessToken;
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }
}
