using BioTrace.Elsa.Abp.Studio.Http;
using BioTrace.Elsa.Abp.Studio.Services;
using NSubstitute;
using Shouldly;
using Xunit;

namespace BioTrace.Elsa.Abp.Studio.Http;

public class AbpTenantHeaderDelegatingHandler_Tests
{
    [Fact]
    public async Task SendAsync_Should_Set_Tenant_Header_With_Guid()
    {
        var tenantId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        var tenantContext = Substitute.For<IAbpStudioTenantContext>();
        tenantContext.CurrentTenantId.Returns(tenantId);

        var handler = new AbpTenantHeaderDelegatingHandler(tenantContext)
        {
            InnerHandler = new StubHandler()
        };

        using var client = new HttpClient(handler);
        using var response = await client.GetAsync("https://example.com/api/test");

        response.RequestMessage!.Headers.TryGetValues("__tenant", out var values).ShouldBeTrue();
        values!.Single().ShouldBe(tenantId.ToString("D"));
    }

    [Fact]
    public async Task SendAsync_Should_Not_Set_Tenant_Header_For_Host()
    {
        var tenantContext = Substitute.For<IAbpStudioTenantContext>();
        tenantContext.CurrentTenantId.Returns((Guid?)null);

        var handler = new AbpTenantHeaderDelegatingHandler(tenantContext)
        {
            InnerHandler = new StubHandler()
        };

        using var client = new HttpClient(handler);
        using var response = await client.GetAsync("https://example.com/api/test");

        response.RequestMessage!.Headers.Contains("__tenant").ShouldBeFalse();
    }

    private sealed class StubHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                RequestMessage = request
            });
        }
    }
}
