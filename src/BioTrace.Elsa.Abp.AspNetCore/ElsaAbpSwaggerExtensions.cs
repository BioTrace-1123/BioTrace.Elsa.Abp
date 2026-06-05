using Elsa.Features.Services;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Builder;

namespace BioTrace.Elsa.Abp;

public static class ElsaAbpSwaggerExtensions
{
    public const string DocumentName = "elsa";

    /// <summary>
    /// OpenAPI JSON path. Uses <c>openapi.json</c> (not <c>swagger.json</c>) so Swashbuckle's
    /// <c>/swagger/{documentName}/swagger.json</c> template does not intercept the request.
    /// </summary>
    public const string OpenApiJsonPath = "/swagger/elsa/openapi.json";

    public const string UiPath = "/swagger/elsa";

    public static void ConfigureElsaSwagger(this IModule elsa, ElsaAbpOptions options)
    {
        elsa.Services.SwaggerDocument(o =>
        {
            o.ExcludeNonFastEndpoints = true;
            o.EnableJWTBearerAuth = true;
            o.AutoTagPathSegmentIndex = 2;
            o.DocumentSettings = s =>
            {
                s.DocumentName = DocumentName;
                s.Title = "Elsa Workflows API";
                s.Version = "v1";
            };
            // Routes are relative to the global "elsa/api" prefix (e.g. "/workflow-definitions").
            o.EndpointFilter = ep => ep.Routes?.Any(r =>
                r.Contains("workflow", StringComparison.OrdinalIgnoreCase)
                || r.Contains("alteration", StringComparison.OrdinalIgnoreCase)
                || r.Contains("task", StringComparison.OrdinalIgnoreCase)) == true;
        });
    }

    private const string OpenApiRouteTemplate = "/swagger/{documentName}/openapi.json";

    public static IApplicationBuilder UseElsaSwagger(this IApplicationBuilder app)
    {
        return app.UseSwaggerGen(
            c => c.Path = OpenApiRouteTemplate,
            ui =>
            {
                ui.Path = UiPath;
                ui.DocumentPath = OpenApiJsonPath;
            });
    }
}
