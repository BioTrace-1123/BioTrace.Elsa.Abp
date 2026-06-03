using Localization.Resources.AbpUi;
using BioTrace.Elsa.Abp.Localization;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace BioTrace.Elsa.Abp;

[DependsOn(
    typeof(AbpApplicationContractsModule),
    typeof(AbpAspNetCoreMvcModule))]
public class AbpHttpApiModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<IMvcBuilder>(mvcBuilder =>
        {
            mvcBuilder.AddApplicationPartIfNotExists(typeof(AbpHttpApiModule).Assembly);
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<AbpResource>()
                .AddBaseTypes(typeof(AbpUiResource));
        });
    }
}
