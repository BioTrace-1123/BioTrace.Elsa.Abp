using BioTrace.Elsa.Abp.Studio.Models;
using Microsoft.Extensions.Options;

namespace BioTrace.Elsa.Abp.Studio.Services;

public class ConfigurationStudioTenantDirectory : IStudioTenantDirectory
{
    private readonly BioTraceElsaAbpStudioOptions _options;

    public ConfigurationStudioTenantDirectory(IOptions<BioTraceElsaAbpStudioOptions> options)
    {
        _options = options.Value;
    }

    public virtual Task<IReadOnlyList<StudioTenantOption>> GetTenantsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<StudioTenantOption> tenants = _options.Tenancy.Tenants;
        return Task.FromResult(tenants);
    }
}
