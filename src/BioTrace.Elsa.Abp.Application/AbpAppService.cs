using BioTrace.Elsa.Abp.Localization;
using Volo.Abp.Application.Services;

namespace BioTrace.Elsa.Abp;

public abstract class AbpAppService : ApplicationService
{
    protected AbpAppService()
    {
        LocalizationResource = typeof(AbpResource);
        ObjectMapperContext = typeof(AbpApplicationModule);
    }
}
