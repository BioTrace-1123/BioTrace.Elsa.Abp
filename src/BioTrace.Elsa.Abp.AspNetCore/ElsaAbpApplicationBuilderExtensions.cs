using Elsa.Extensions;
using Microsoft.AspNetCore.Builder;

namespace BioTrace.Elsa.Abp;

public static class ElsaAbpApplicationBuilderExtensions
{
    public static IApplicationBuilder UseElsaWorkflows(this IApplicationBuilder app)
    {
        app.UseWorkflowsApi();
        return app;
    }
}
