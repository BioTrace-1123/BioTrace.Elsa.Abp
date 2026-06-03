using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Modularity;
using VoloAbpEntityFrameworkCoreModule = Volo.Abp.EntityFrameworkCore.AbpEntityFrameworkCoreModule;

namespace BioTrace.Elsa.Abp.EntityFrameworkCore;

[DependsOn(
    typeof(AbpDomainModule),
    typeof(VoloAbpEntityFrameworkCoreModule)
)]
public class AbpEntityFrameworkCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAbpDbContext<AbpDbContext>(options =>
        {
            options.AddDefaultRepositories<IAbpDbContext>(includeAllEntities: true);
            
            /* Add custom repositories here. Example:
            * options.AddRepository<Question, EfCoreQuestionRepository>();
            */
        });
    }
}
