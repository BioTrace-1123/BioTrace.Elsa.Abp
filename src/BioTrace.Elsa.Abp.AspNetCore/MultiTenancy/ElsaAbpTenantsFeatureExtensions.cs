using Elsa.Common.Features;
using Elsa.Tenants.Features;

namespace BioTrace.Elsa.Abp.MultiTenancy;

public static class ElsaAbpTenantsFeatureExtensions
{
    public static TenantsFeature UseAbpTenantsProvider(this TenantsFeature feature)
    {
        feature.Module.Configure<MultitenancyFeature>(multitenancy =>
            multitenancy.UseTenantsProvider<ElsaAbpTenantsProvider>());

        return feature;
    }
}
