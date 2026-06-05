using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;

namespace BioTrace.Elsa.Abp.Fixtures;

/// <summary>
/// OpenIddict token endpoint requires HTTPS; TestServer uses HTTP unless forwarded proto is honored.
/// </summary>
public class IntegrationTestHttpsStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedProto
            });
            next(app);
        };
    }
}
