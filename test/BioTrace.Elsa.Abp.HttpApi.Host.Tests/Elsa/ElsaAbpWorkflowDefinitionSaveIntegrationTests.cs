using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BioTrace.Elsa.Abp.Fixtures;
using BioTrace.Elsa.Abp.Helpers;
using BioTrace.Elsa.Abp.IntegrationTesting;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Xunit;

namespace BioTrace.Elsa.Abp.Elsa;

[Trait("Category", "Integration")]
[Collection(IntegrationTestCollection.Name)]
public class ElsaAbpWorkflowDefinitionSaveIntegrationTests : IAsyncLifetime
{
    private readonly ElsaAbpWebApplicationFactory _factory;
    private HttpClient _client = null!;
    private ElsaAbpIntegrationTestTokenClient _tokenClient = null!;

    public ElsaAbpWorkflowDefinitionSaveIntegrationTests(ElsaAbpWebApplicationFactory factory)
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
        _tokenClient = new ElsaAbpIntegrationTestTokenClient(_client);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Save_workflow_definition_should_return_json_body(bool publish)
    {
        var definitionId = $"SaveResponseTest-{publish}-{Guid.NewGuid():N}";
        var token = await _tokenClient.RequestPasswordTokenAsync("admin");

        using var request = CreateAuthorizedRequest(HttpMethod.Post, "/elsa/api/workflow-definitions", token);
        request.Content = CreateSaveWorkflowDefinitionContent(definitionId, "Save Response Test", publish);

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldNotBeNullOrWhiteSpace();

        var payload = await response.Content.ReadFromJsonAsync<SaveWorkflowDefinitionResponse>();
        payload.ShouldNotBeNull();
        payload!.WorkflowDefinition.ShouldNotBeNull();
        payload.WorkflowDefinition.DefinitionId.ShouldBe(definitionId);
    }

    private static HttpRequestMessage CreateAuthorizedRequest(
        HttpMethod method,
        string url,
        string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private static JsonContent CreateSaveWorkflowDefinitionContent(string definitionId, string name, bool publish)
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
            publish
        });
    }
}
