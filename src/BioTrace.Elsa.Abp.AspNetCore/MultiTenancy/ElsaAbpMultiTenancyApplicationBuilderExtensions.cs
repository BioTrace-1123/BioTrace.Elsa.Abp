using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BioTrace.Elsa.Abp.MultiTenancy;

public static class ElsaAbpMultiTenancyApplicationBuilderExtensions
{
    public static IApplicationBuilder UseElsaAbpMultiTenancy(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ElsaAbpMultiTenancyMiddleware>();
    }
}
