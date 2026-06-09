using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BioTrace.Elsa.Abp.Fixtures;
using BioTrace.Elsa.Abp.Helpers;
using BioTrace.Elsa.Abp.MultiTenancy;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Xunit;

namespace BioTrace.Elsa.Abp.MultiTenancy;

[Trait("Category", "Integration")]
[Collection(IntegrationTestCollection.Name)]
public class ElsaAbpMultiTenancyIntegrationTests : IAsyncLifetime
{
    private readonly ElsaAbpWebApplicationFactory _factory;
    private HttpClient _client = null!;
    private OpenIddictTokenClient _tokenClient = null!;

    public ElsaAbpMultiTenancyIntegrationTests(ElsaAbpWebApplicationFactory factory)
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
    public async Task Tenant_token_should_include_tenantid_claim()
    {
        var token = await _tokenClient.RequestPasswordTokenAsync(
            ElsaAbpMultiTenancySeedData.TenantAAdminUserName,
            ElsaAbpMultiTenancySeedData.TenantAName);

        var tenantClaims = JwtPayloadReader.ReadClaims(token)
            .Where(c => string.Equals(c.Type, "tenantid", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Value)
            .ToList();

        tenantClaims.Count.ShouldBe(1);
        tenantClaims[0].ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Workflow_definitions_should_be_isolated_between_tenants()
    {
        const string definitionId = "MultiTenancyIsolationTest";

        var tenantAToken = await _tokenClient.RequestPasswordTokenAsync(
            ElsaAbpMultiTenancySeedData.TenantAAdminUserName,
            ElsaAbpMultiTenancySeedData.TenantAName);

        using (var createRequest = CreateAuthorizedRequest(
                   HttpMethod.Post,
                   "/elsa/api/workflow-definitions",
                   tenantAToken,
                   ElsaAbpMultiTenancySeedData.TenantAName))
        {
            createRequest.Content = CreateSaveWorkflowDefinitionContent(
                definitionId,
                "Tenant A Workflow");

            var createResponse = await _client.SendAsync(createRequest);
            createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        var tenantBToken = await _tokenClient.RequestPasswordTokenAsync(
            ElsaAbpMultiTenancySeedData.TenantBAdminUserName,
            ElsaAbpMultiTenancySeedData.TenantBName);

        using var listRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/elsa/api/workflow-definitions",
            tenantBToken,
            ElsaAbpMultiTenancySeedData.TenantBName);

        var listResponse = await _client.SendAsync(listRequest);
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await listResponse.Content.ReadFromJsonAsync<WorkflowDefinitionListResponse>();
        payload.ShouldNotBeNull();
        payload!.Items.ShouldNotContain(item =>
            string.Equals(item.DefinitionId, definitionId, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Host_admin_should_not_see_tenant_workflow_definitions()
    {
        const string definitionId = "HostIsolationTest";

        var tenantAToken = await _tokenClient.RequestPasswordTokenAsync(
            ElsaAbpMultiTenancySeedData.TenantAAdminUserName,
            ElsaAbpMultiTenancySeedData.TenantAName);

        using (var createRequest = CreateAuthorizedRequest(
                   HttpMethod.Post,
                   "/elsa/api/workflow-definitions",
                   tenantAToken,
                   ElsaAbpMultiTenancySeedData.TenantAName))
        {
            createRequest.Content = CreateSaveWorkflowDefinitionContent(
                definitionId,
                "Tenant Only Workflow");

            (await _client.SendAsync(createRequest)).StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        var hostAdminToken = await _tokenClient.RequestPasswordTokenAsync("admin");

        using var listRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/elsa/api/workflow-definitions",
            hostAdminToken);

        var listResponse = await _client.SendAsync(listRequest);
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await listResponse.Content.ReadFromJsonAsync<WorkflowDefinitionListResponse>();
        payload.ShouldNotBeNull();
        payload!.Items.ShouldNotContain(item =>
            string.Equals(item.DefinitionId, definitionId, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Host_admin_with_tenant_header_should_see_tenant_workflow_definitions()
    {
        const string definitionId = "HostImpersonationTest";

        var tenantAToken = await _tokenClient.RequestPasswordTokenAsync(
            ElsaAbpMultiTenancySeedData.TenantAAdminUserName,
            ElsaAbpMultiTenancySeedData.TenantAName);

        using (var createRequest = CreateAuthorizedRequest(
                   HttpMethod.Post,
                   "/elsa/api/workflow-definitions",
                   tenantAToken,
                   ElsaAbpMultiTenancySeedData.TenantAName))
        {
            createRequest.Content = CreateSaveWorkflowDefinitionContent(
                definitionId,
                "Host Impersonation Test Workflow");

            (await _client.SendAsync(createRequest)).StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        var hostAdminToken = await _tokenClient.RequestPasswordTokenAsync("admin");

        using var listRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/elsa/api/workflow-definitions",
            hostAdminToken,
            ElsaAbpMultiTenancySeedData.TenantAName);

        var listResponse = await _client.SendAsync(listRequest);
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await listResponse.Content.ReadFromJsonAsync<WorkflowDefinitionListResponse>();
        payload.ShouldNotBeNull();
        payload!.Items.ShouldContain(item =>
            string.Equals(item.DefinitionId, definitionId, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Host_admin_with_other_tenant_header_should_not_see_tenant_workflow_definitions()
    {
        const string definitionId = "HostImpersonationIsolationTest";

        var tenantAToken = await _tokenClient.RequestPasswordTokenAsync(
            ElsaAbpMultiTenancySeedData.TenantAAdminUserName,
            ElsaAbpMultiTenancySeedData.TenantAName);

        using (var createRequest = CreateAuthorizedRequest(
                   HttpMethod.Post,
                   "/elsa/api/workflow-definitions",
                   tenantAToken,
                   ElsaAbpMultiTenancySeedData.TenantAName))
        {
            createRequest.Content = CreateSaveWorkflowDefinitionContent(
                definitionId,
                "Tenant A Only For Host Header Test");

            (await _client.SendAsync(createRequest)).StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        var hostAdminToken = await _tokenClient.RequestPasswordTokenAsync("admin");

        using var listRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/elsa/api/workflow-definitions",
            hostAdminToken,
            ElsaAbpMultiTenancySeedData.TenantBName);

        var listResponse = await _client.SendAsync(listRequest);
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await listResponse.Content.ReadFromJsonAsync<WorkflowDefinitionListResponse>();
        payload.ShouldNotBeNull();
        payload!.Items.ShouldNotContain(item =>
            string.Equals(item.DefinitionId, definitionId, StringComparison.OrdinalIgnoreCase));
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

    private static JsonContent CreateSaveWorkflowDefinitionContent(string definitionId, string name)
    {
        return JsonContent.Create(new
        {
            model = new
            {
                definitionId,
                name,
                root = new
                {
                    type = "Elsa.Sequence",
                    id = "root",
                    version = 1
                }
            },
            publish = false
        });
    }

    private sealed class WorkflowDefinitionListResponse
    {
        [JsonPropertyName("items")]
        public List<WorkflowDefinitionListItem> Items { get; set; } = [];

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }
    }

    private sealed class WorkflowDefinitionListItem
    {
        [JsonPropertyName("definitionId")]
        public string? DefinitionId { get; set; }
    }
}
