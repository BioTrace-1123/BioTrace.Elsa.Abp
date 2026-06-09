using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BioTrace.Elsa.Abp.Elsa;
using BioTrace.Elsa.Abp.Fixtures;
using BioTrace.Elsa.Abp.Helpers;
using BioTrace.Elsa.Abp.MultiTenancy;
using BioTrace.Elsa.Abp.Permissions;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Xunit;

namespace BioTrace.Elsa.Abp.Security;

[Trait("Category", "Integration")]
[Collection(IntegrationTestCollection.Name)]
public class ElsaAbpPermissionBridgeIntegrationTests : IAsyncLifetime
{
    private readonly ElsaAbpWebApplicationFactory _factory;
    private HttpClient _client = null!;
    private OpenIddictTokenClient _tokenClient = null!;

    public ElsaAbpPermissionBridgeIntegrationTests(ElsaAbpWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await ElsaAbpWebApplicationFactory.EnsurePostgresReadyAsync();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
        _tokenClient = new OpenIddictTokenClient(_client);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Should_return_401_without_token()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/elsa/api/workflow-definitions");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Designer_token_should_not_contain_wildcard_in_jwt()
    {
        var token = await _tokenClient.RequestPasswordTokenAsync(
            ElsaAbpMultiTenancySeedData.TenantADesignerUserName,
            ElsaAbpMultiTenancySeedData.TenantAName);

        var permissionClaims = JwtPayloadReader.ReadClaims(token)
            .Where(c => string.Equals(c.Type, "permissions", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Value)
            .ToList();

        // Admin JWT may include "*" from ABP dynamic claims; designer must remain least-privilege in the token.
        permissionClaims.ShouldNotContain(ElsaApiPermissionNames.Wildcard);
        permissionClaims.ShouldNotContain(ElsaApiPermissionNames.WorkflowDefinitions.Write);
    }

    [Fact]
    public async Task Admin_should_get_wildcard_permissions_from_current_user()
    {
        var token = await _tokenClient.RequestPasswordTokenAsync("admin");
        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/identity/users/me",
            token,
            ElsaAbpMultiTenancySeedData.TenantAName);

        var response = await _client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var currentUser = await response.Content.ReadFromJsonAsync<ElsaAbpCurrentUserDto>();
        currentUser.ShouldNotBeNull();
        currentUser!.Permissions.ShouldContain(ElsaApiPermissionNames.Wildcard);
    }

    [Fact]
    public async Task Admin_should_read_workflow_definitions()
    {
        var token = await _tokenClient.RequestPasswordTokenAsync("admin");
        using var request = CreateAuthorizedRequest(HttpMethod.Get, "/elsa/api/workflow-definitions", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Designer_should_get_read_only_permissions()
    {
        var token = await _tokenClient.RequestPasswordTokenAsync(
            ElsaAbpMultiTenancySeedData.TenantADesignerUserName,
            ElsaAbpMultiTenancySeedData.TenantAName);
        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/identity/users/me",
            token,
            ElsaAbpMultiTenancySeedData.TenantAName);

        var response = await _client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var currentUser = await response.Content.ReadFromJsonAsync<ElsaAbpCurrentUserDto>();
        currentUser.ShouldNotBeNull();
        currentUser!.Permissions.ShouldContain(ElsaApiPermissionNames.WorkflowDefinitions.Read);
        currentUser.Permissions.ShouldContain(ElsaApiPermissionNames.WorkflowInstances.Read);
        currentUser.Permissions.ShouldNotContain(ElsaApiPermissionNames.Wildcard);
        currentUser.Permissions.ShouldNotContain(ElsaApiPermissionNames.WorkflowDefinitions.Write);
    }

    [Fact]
    public async Task Designer_should_be_forbidden_on_create_workflow_definition()
    {
        var token = await _tokenClient.RequestPasswordTokenAsync(
            ElsaAbpMultiTenancySeedData.TenantADesignerUserName,
            ElsaAbpMultiTenancySeedData.TenantAName);
        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/elsa/api/workflow-definitions",
            token,
            ElsaAbpMultiTenancySeedData.TenantAName);
        request.Content = JsonContent.Create(new
        {
            name = "integration-test-definition",
            definitionId = "IntegrationTestDefinition",
            root = new
            {
                type = "Elsa.Sequence",
                version = 1
            }
        });

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static HttpRequestMessage CreateAuthorizedRequest(
        HttpMethod method,
        string url,
        string token,
        string? tenantName = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (!string.IsNullOrWhiteSpace(tenantName))
        {
            request.Headers.TryAddWithoutValidation("__tenant", tenantName);
        }

        return request;
    }
}
