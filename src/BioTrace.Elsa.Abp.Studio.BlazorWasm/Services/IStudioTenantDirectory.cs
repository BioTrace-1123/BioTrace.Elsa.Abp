using BioTrace.Elsa.Abp.Studio.Models;

namespace BioTrace.Elsa.Abp.Studio.Services;

public interface IStudioTenantDirectory
{
    Task<IReadOnlyList<StudioTenantOption>> GetTenantsAsync(CancellationToken cancellationToken = default);
}
