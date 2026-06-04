using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace BioTrace.Elsa.Abp.Elsa;

[Area(AbpRemoteServiceConsts.ModuleName)]
[RemoteService(Name = AbpRemoteServiceConsts.RemoteServiceName)]
[Route("api/abp/elsa")]
public class ElsaAbpCurrentUserController : AbpController, IElsaAbpCurrentUserAppService
{
    private readonly IElsaAbpCurrentUserAppService _currentUserAppService;

    public ElsaAbpCurrentUserController(IElsaAbpCurrentUserAppService currentUserAppService)
    {
        _currentUserAppService = currentUserAppService;
    }

    [HttpGet]
    [Route("current-user")]
    [Authorize]
    public virtual Task<ElsaAbpCurrentUserDto> GetAsync()
    {
        return _currentUserAppService.GetAsync();
    }

    /// <summary>Compatibility alias for Elsa Studio /identity style paths.</summary>
    [HttpGet]
    [Route("/identity/users/me")]
    [Authorize]
    public virtual Task<ElsaAbpCurrentUserDto> GetMeAsync()
    {
        return _currentUserAppService.GetAsync();
    }
}
