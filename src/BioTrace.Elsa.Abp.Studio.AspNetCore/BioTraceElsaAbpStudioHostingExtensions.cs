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

        if (app is not WebApplication webApp)
        {
            throw new InvalidOperationException(
                "BioTrace Elsa Studio hosting requires WebApplication. Ensure the host uses WebApplication.CreateBuilder.");
        }

        var pathBase = NormalizePathBase(options.PathBase);
        var fallbackPattern = $"{pathBase.TrimStart('/')}/{{*path:nonfile}}";

        app.UseWhen(
            ctx => ctx.Request.Path.StartsWithSegments(pathBase),
            studioBranch =>
            {
                studioBranch.UseBlazorFrameworkFiles(pathBase);
                studioBranch.UseStaticFiles();
            });

        webApp.MapGet(pathBase, () => Results.Redirect(pathBase + "/"));
        webApp.MapFallbackToFile(fallbackPattern, "index.html");

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
