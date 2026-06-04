using Volo.Abp.Application.Services;

namespace BioTrace.Elsa.Abp.Elsa;

public interface IElsaAbpCurrentUserAppService : IApplicationService
{
    Task<ElsaAbpCurrentUserDto> GetAsync();
}
