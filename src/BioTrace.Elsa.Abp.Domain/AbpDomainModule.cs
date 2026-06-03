using Volo.Abp.Domain;
using Volo.Abp.Modularity;

namespace BioTrace.Elsa.Abp;

[DependsOn(
    typeof(AbpDddDomainModule),
    typeof(AbpDomainSharedModule)
)]
public class AbpDomainModule : AbpModule
{

}
