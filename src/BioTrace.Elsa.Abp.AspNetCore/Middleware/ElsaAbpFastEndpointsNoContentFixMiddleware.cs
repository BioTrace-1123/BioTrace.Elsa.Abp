using Microsoft.AspNetCore.Http;

namespace BioTrace.Elsa.Abp.Middleware;

/// <summary>
/// Fixes Elsa FastEndpoints endpoints that write JSON via <c>WriteAsJsonAsync</c> but leave
/// <c>Response</c> unset, causing FastEndpoints to auto-send 204 No Content with an empty body.
/// </summary>
public class ElsaAbpFastEndpointsNoContentFixMiddleware
{
    private readonly RequestDelegate _next;

    public ElsaAbpFastEndpointsNoContentFixMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public virtual async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldBufferResponse(context.Request))
        {
            await _next(context);
            return;
        }

        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);

            buffer.Position = 0;
            var body = buffer.ToArray();

            if (ShouldFixNoContentResponse(context.Response.StatusCode, body))
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "application/json; charset=utf-8";
                context.Response.ContentLength = body.Length;
            }

            context.Response.Body = originalBody;
            if (body.Length > 0)
            {
                await originalBody.WriteAsync(body, context.RequestAborted);
            }
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    protected virtual bool ShouldBufferResponse(HttpRequest request)
    {
        if (!HttpMethods.IsPost(request.Method) && !HttpMethods.IsPut(request.Method))
        {
            return false;
        }

        return request.Path.StartsWithSegments("/elsa/api", StringComparison.OrdinalIgnoreCase);
    }

    protected virtual bool ShouldFixNoContentResponse(int statusCode, ReadOnlySpan<byte> body)
    {
        return statusCode == StatusCodes.Status204NoContent && body.Length > 0;
    }
}
