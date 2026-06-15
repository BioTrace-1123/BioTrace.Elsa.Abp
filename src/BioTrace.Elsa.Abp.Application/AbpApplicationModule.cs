using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Mapperly;
using Volo.Abp.Modularity;
using Volo.Abp.Application;
using BioTrace.Elsa.Abp.Data;

namespace BioTrace.Elsa.Abp;

[DependsOn(
    typeof(AbpDomainModule),
    typeof(AbpApplicationContractsModule),
    typeof(AbpDddApplicationModule),
    typeof(AbpMapperlyModule)
    )]
public class AbpApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddMapperlyObjectMapper<AbpApplicationModule>();
        Configure<ElsaAbpPermissionSeedOptions>(_ => { });
    }
}
