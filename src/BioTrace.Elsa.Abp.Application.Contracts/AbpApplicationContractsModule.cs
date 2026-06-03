using Volo.Abp.Application;
using Volo.Abp.Modularity;
using Volo.Abp.Authorization;

namespace BioTrace.Elsa.Abp;

[DependsOn(
    typeof(AbpDomainSharedModule),
    typeof(AbpDddApplicationContractsModule),
    typeof(AbpAuthorizationModule)
    )]
public class AbpApplicationContractsModule : AbpModule
{

}
