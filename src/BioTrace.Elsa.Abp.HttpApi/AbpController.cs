using BioTrace.Elsa.Abp.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace BioTrace.Elsa.Abp;

public abstract class AbpController : AbpControllerBase
{
    protected AbpController()
    {
        LocalizationResource = typeof(AbpResource);
    }
}
