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

        var pathBase = NormalizePathBase(options.PathBase);
        var studioContentPrefix = $"{pathBase}/_content";

        // Package _content assets stay at /_content while index.html uses <base href="/studio/">.
        app.Use(async (context, next) =>
        {
            var requestPath = context.Request.Path.Value;
            if (requestPath?.StartsWith(studioContentPrefix, StringComparison.OrdinalIgnoreCase) == true)
            {
                context.Request.Path = new PathString("/_content" + requestPath[studioContentPrefix.Length..]);
            }

            await next();
        });

        return app;
    }

    public static IApplicationBuilder UseBioTraceElsaAbpStudioFallback(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices
            .GetRequiredService<IOptions<BioTraceElsaAbpStudioOptions>>()
            .Value;

        if (!options.Enabled || app is not WebApplication webApp)
        {
            return app;
        }

        var pathBase = NormalizePathBase(options.PathBase);
        var assetPathBase = pathBase.TrimStart('/');

        // WASM assets under studio/_framework are served by UseStaticFiles + MapStaticAssets.
        webApp.MapGroup(pathBase).MapFallbackToFile(
            "{*path:nonfile}",
            $"{assetPathBase}/index.html");

        return app;
    }

    private static string NormalizePathBase(string pathBase)
    {
        if (string.IsNullOrWhiteSpace(pathBase))
        {
            return "/studio";
        }

        var normalized = pathBase.Trim();
        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized;
        }

        return normalized.TrimEnd('/');
    }
}
