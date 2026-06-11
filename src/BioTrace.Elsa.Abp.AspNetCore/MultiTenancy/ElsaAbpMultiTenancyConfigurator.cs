using Elsa.Common.Multitenancy;
using Elsa.Features.Services;
using Elsa.Tenants.Extensions;

namespace BioTrace.Elsa.Abp.MultiTenancy;

public static class ElsaAbpMultiTenancyConfigurator
{
    public static void Configure(IModule elsa, ElsaAbpOptions options)
    {
        if (!options.EnableMultiTenancy)
        {
            return;
        }

        elsa.UseTenants(tenants =>
        {
            tenants.ConfigureTenants(tenantOptions =>
            {
                tenantOptions.IsEnabled = true;
            });

            tenants.ConfigureMultitenancy(multitenancy =>
            {
                multitenancy.TenantResolverPipelineBuilder.Clear();
                multitenancy.TenantResolverPipelineBuilder.Append<ElsaAbpCurrentTenantResolver>();
            });

            tenants.UseAbpTenantsProvider();
        });
    }
}
