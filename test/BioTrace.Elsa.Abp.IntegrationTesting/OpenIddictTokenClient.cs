using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace BioTrace.Elsa.Abp.IntegrationTesting;

public class OpenIddictTokenClient
{
    public const string DefaultPassword = "1q2w3E*";

    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _scope;

    public OpenIddictTokenClient(
        HttpClient httpClient,
        string clientId,
        string clientSecret,
        string scope)
    {
        _httpClient = httpClient;
        _clientId = clientId;
        _clientSecret = clientSecret;
        _scope = scope;
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
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
                ["username"] = username,
                ["password"] = password,
                ["scope"] = _scope
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
