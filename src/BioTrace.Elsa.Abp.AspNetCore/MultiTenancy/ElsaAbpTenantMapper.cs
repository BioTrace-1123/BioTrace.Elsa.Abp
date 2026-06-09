using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace BioTrace.Elsa.Abp.MultiTenancy;

public class ElsaAbpTenantMapper : IElsaAbpTenantMapper, ISingletonDependency
{
    private readonly ElsaAbpOptions _options;

    public ElsaAbpTenantMapper(IOptions<ElsaAbpOptions> options)
    {
        _options = options.Value;
    }

    public virtual string ToElsaTenantId(Guid? abpTenantId)
    {
        if (abpTenantId == null)
        {
            return _options.HostTenantId;
        }

        return abpTenantId.Value.ToString("D");
    }

    public virtual Guid? ToAbpTenantId(string? elsaTenantId)
    {
        if (string.IsNullOrEmpty(elsaTenantId) || elsaTenantId == _options.HostTenantId)
        {
            return null;
        }

        return Guid.TryParse(elsaTenantId, out var tenantId) ? tenantId : null;
    }
}
