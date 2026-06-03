using Volo.Abp.Modularity;

namespace BioTrace.Elsa.Abp;

[DependsOn(
    typeof(AbpDomainModule),
    typeof(AbpTestBaseModule)
)]
public class AbpDomainTestModule : AbpModule
{

}
