using Elsa.Extensions;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BioTrace.Elsa.Abp;

public static class ElsaAbpApplicationBuilderExtensions
{
    public static IApplicationBuilder UseElsaWorkflows(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetRequiredService<IOptions<ElsaAbpOptions>>().Value;

        if (!options.EnableWorkflowsApi)
        {
            return app;
        }

        // UseWorkflowsApi() already calls UseFastEndpoints; configure the shared singleton first.
        var fastEndpointsConfig = app.ApplicationServices.GetRequiredService<Config>();
        fastEndpointsConfig.Security.PermissionsClaimType = options.PermissionsClaimType;
        fastEndpointsConfig.Security.RoleClaimType = options.RoleClaimType;

        app.UseWorkflowsApi();

        if (options.EnableElsaSwagger)
        {
            app.UseElsaSwagger();
        }

        return app;
    }
}
