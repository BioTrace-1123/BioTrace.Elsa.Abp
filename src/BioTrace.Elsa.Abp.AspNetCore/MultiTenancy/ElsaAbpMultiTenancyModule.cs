using Elsa.Common.Multitenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;

namespace BioTrace.Elsa.Abp.MultiTenancy;

[DependsOn(
    typeof(ElsaAbpAspNetCoreModule),
    typeof(AbpMultiTenancyModule),
    typeof(AbpTenantManagementDomainModule))]
public class ElsaAbpMultiTenancyModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<ElsaAbpOptions>(options =>
        {
            options.EnableMultiTenancy = true;
        });

        Configure<AbpMultiTenancyOptions>(options =>
        {
            options.IsEnabled = MultiTenancyConsts.IsEnabled;
        });

        context.Services.AddHttpContextAccessor();
        context.Services.Replace(ServiceDescriptor.Singleton<ITenantAccessor, ElsaAbpTenantAccessor>());
        context.Services.Replace(ServiceDescriptor.Transient<ITenantsProvider, ElsaAbpTenantsProvider>());
    }
}
