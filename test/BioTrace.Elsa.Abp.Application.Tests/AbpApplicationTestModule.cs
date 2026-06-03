using Volo.Abp.Modularity;

namespace BioTrace.Elsa.Abp;

[DependsOn(
    typeof(AbpApplicationModule),
    typeof(AbpDomainTestModule)
    )]
public class AbpApplicationTestModule : AbpModule
{

}
