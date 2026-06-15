using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BioTrace.Elsa.Abp.Studio;

public static class BioTraceElsaAbpStudioHostingExtensions
{
    public static IServiceCollection AddBioTraceElsaAbpStudioHost(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BioTraceElsaAbpStudioOptions>(configuration.GetSection("ElsaStudio"));
        return services;
    }

    public static IApplicationBuilder UseBioTraceElsaAbpStudioHost(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices
            .GetRequiredService<IOptions<BioTraceElsaAbpStudioOptions>>()
            .Value;

        if (!options.Enabled)
        {
            return app;
        }

        if (app is not WebApplication)
        {
            throw new InvalidOperationException(
                "BioTrace Elsa Studio hosting requires WebApplication. Ensure the host uses WebApplication.CreateBuilder.");
        }

        var pathBase = BioTraceElsaAbpStudioPaths.NormalizePathBase(options.PathBase);
        var assetPathBase = pathBase.TrimStart('/');
        var studioContentPrefix = $"{pathBase}/_content";
        var studioIndexPath = $"/{assetPathBase}/index.html";

        // Package _content assets stay at /_content while index.html uses <base href="/studio/">.
        app.Use(async (context, next) =>
        {
            var requestPath = context.Request.Path.Value;
            if (BioTraceElsaAbpStudioPaths.TryGetLegacyAuthenticationRedirect(
                    requestPath,
                    pathBase,
                    out var legacyAuthenticationRedirect))
            {
                var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
                context.Response.Redirect(legacyAuthenticationRedirect + query, permanent: false);
                return;
            }

            if (requestPath?.StartsWith(studioContentPrefix, StringComparison.OrdinalIgnoreCase) == true)
            {
                context.Request.Path = new PathString("/_content" + requestPath[studioContentPrefix.Length..]);
            }
            else if (ShouldServeStudioIndex(requestPath, pathBase, context.Request.Method))
            {
                // MapFallbackToFile("{*path:nonfile}") does not match bare /studio or extensionless SPA routes.
                context.Request.Path = new PathString(studioIndexPath);
            }

            await next();
        });

        return app;
    }

    [Obsolete("SPA 路由已在 UseBioTraceElsaAbpStudioHost 处理，将在 2.0 移除。")]
    public static IApplicationBuilder UseBioTraceElsaAbpStudioFallback(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices
            .GetRequiredService<IOptions<BioTraceElsaAbpStudioOptions>>()
            .Value;

        if (!options.Enabled)
        {
            return app;
        }

        // SPA routes are rewritten to index.html in UseBioTraceElsaAbpStudioHost; static assets serve the file.
        return app;
    }

    private static bool ShouldServeStudioIndex(string? requestPath, string pathBase, string method)
    {
        if (!HttpMethods.IsGet(method) && !HttpMethods.IsHead(method))
        {
            return false;
        }

        if (string.IsNullOrEmpty(requestPath)
            || !requestPath.StartsWith(pathBase, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (requestPath.Length == pathBase.Length)
        {
            return true;
        }

        if (requestPath[pathBase.Length] != '/')
        {
            return false;
        }

        var relativePath = requestPath[(pathBase.Length + 1)..];
        return !Path.HasExtension(relativePath);
    }
}
