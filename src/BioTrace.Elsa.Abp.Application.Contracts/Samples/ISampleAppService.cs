using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace BioTrace.Elsa.Abp.Samples;

public interface ISampleAppService : IApplicationService
{
    Task<SampleDto> GetAsync();

    Task<SampleDto> GetAuthorizedAsync();
}
