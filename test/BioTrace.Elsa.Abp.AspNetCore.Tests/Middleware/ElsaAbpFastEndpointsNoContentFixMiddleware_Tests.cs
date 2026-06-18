using System.Text;
using BioTrace.Elsa.Abp.Middleware;
using Microsoft.AspNetCore.Http;
using Shouldly;
using Xunit;

namespace BioTrace.Elsa.Abp.Middleware;

public class ElsaAbpFastEndpointsNoContentFixMiddleware_Tests
{
    [Fact]
    public async Task Should_not_buffer_GET_requests()
    {
        var context = CreateContext(HttpMethods.Get, "/elsa/api/workflow-definitions");
        var originalBody = context.Response.Body;
        var downstreamInvoked = false;

        var middleware = new ElsaAbpFastEndpointsNoContentFixMiddleware(_ =>
        {
            downstreamInvoked = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        downstreamInvoked.ShouldBeTrue();
        context.Response.Body.ShouldBeSameAs(originalBody);
    }

    [Fact]
    public async Task Should_fix_204_with_buffered_json_body()
    {
        const string json = """{"workflowDefinition":{"definitionId":"test"},"alreadyPublished":false,"consumingWorkflowCount":0}""";
        var context = CreateContext(HttpMethods.Post, "/elsa/api/workflow-definitions");
        var responseStream = new MemoryStream();
        context.Response.Body = responseStream;

        var middleware = new ElsaAbpFastEndpointsNoContentFixMiddleware(async innerContext =>
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            await innerContext.Response.Body.WriteAsync(bytes);
            innerContext.Response.StatusCode = StatusCodes.Status204NoContent;
        });

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        context.Response.ContentType.ShouldBe("application/json; charset=utf-8");
        context.Response.ContentLength.ShouldBe(Encoding.UTF8.GetByteCount(json));

        responseStream.Position = 0;
        using var reader = new StreamReader(responseStream, Encoding.UTF8);
        (await reader.ReadToEndAsync()).ShouldBe(json);
    }

    [Fact]
    public async Task Should_passthrough_non_204_responses()
    {
        const string json = """{"ok":true}""";
        var context = CreateContext(HttpMethods.Post, "/elsa/api/workflow-definitions");
        var responseStream = new MemoryStream();
        context.Response.Body = responseStream;

        var middleware = new ElsaAbpFastEndpointsNoContentFixMiddleware(async innerContext =>
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            await innerContext.Response.Body.WriteAsync(bytes);
            innerContext.Response.StatusCode = StatusCodes.Status200OK;
            innerContext.Response.ContentType = "application/json";
        });

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        context.Response.ContentType.ShouldBe("application/json");

        responseStream.Position = 0;
        using var reader = new StreamReader(responseStream, Encoding.UTF8);
        (await reader.ReadToEndAsync()).ShouldBe(json);
    }

    [Fact]
    public async Task Should_leave_empty_204_unchanged()
    {
        var context = CreateContext(HttpMethods.Post, "/elsa/api/workflow-definitions");
        var responseStream = new MemoryStream();
        context.Response.Body = responseStream;

        var middleware = new ElsaAbpFastEndpointsNoContentFixMiddleware(innerContext =>
        {
            innerContext.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status204NoContent);
        responseStream.Length.ShouldBe(0);
    }

    private static DefaultHttpContext CreateContext(string method, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }
}
