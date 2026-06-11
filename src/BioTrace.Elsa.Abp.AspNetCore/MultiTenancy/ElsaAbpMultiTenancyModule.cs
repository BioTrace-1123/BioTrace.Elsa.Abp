using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;

namespace BioTrace.Elsa.Abp.MultiTenancy;

[DependsOn(
    typeof(AbpMultiTenancyModule),
    typeof(AbpTenantManagementDomainModule))]
public class ElsaAbpMultiTenancyModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        Configure<ElsaAbpOptions>(options => options.EnableMultiTenancy = true);
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpMultiTenancyOptions>(options =>
        {
            options.IsEnabled = MultiTenancyConsts.IsEnabled;
        });

        Configure<AbpTenantResolveOptions>(options =>
        {
            options.TenantResolvers.Insert(0, new ElsaAbpHostTenantHeaderResolveContributor());
        });
    }
}
