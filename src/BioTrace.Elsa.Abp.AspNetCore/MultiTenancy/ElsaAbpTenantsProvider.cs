using Elsa.Common.Multitenancy;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;
using ElsaTenant = Elsa.Common.Multitenancy.Tenant;

namespace BioTrace.Elsa.Abp.MultiTenancy;

public class ElsaAbpTenantsProvider : ITenantsProvider, ITransientDependency
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IElsaAbpTenantMapper _tenantMapper;
    private readonly ICurrentTenant _currentTenant;

    public ElsaAbpTenantsProvider(
        ITenantRepository tenantRepository,
        IElsaAbpTenantMapper tenantMapper,
        ICurrentTenant currentTenant)
    {
        _tenantRepository = tenantRepository;
        _tenantMapper = tenantMapper;
        _currentTenant = currentTenant;
    }

    public virtual async Task<IEnumerable<ElsaTenant>> ListAsync(CancellationToken cancellationToken = default)
    {
        var tenants = new List<ElsaTenant>
        {
            CreateHostTenant()
        };

        using (_currentTenant.Change(null))
        {
            var abpTenants = await _tenantRepository.GetListAsync(cancellationToken: cancellationToken);
            tenants.AddRange(abpTenants.Select(MapAbpTenant));
        }

        return tenants;
    }

    public virtual async Task<ElsaTenant?> FindAsync(TenantFilter filter, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(filter.Id))
        {
            return CreateHostTenant();
        }

        var abpTenantId = _tenantMapper.ToAbpTenantId(filter.Id);
        if (abpTenantId == null)
        {
            return CreateHostTenant();
        }

        using (_currentTenant.Change(null))
        {
            var abpTenant = await _tenantRepository.FindAsync(abpTenantId.Value, cancellationToken: cancellationToken);
            return abpTenant == null ? null : MapAbpTenant(abpTenant);
        }
    }

    protected virtual ElsaTenant CreateHostTenant()
    {
        var hostTenantId = _tenantMapper.ToElsaTenantId(null);

        return new ElsaTenant
        {
            Id = hostTenantId,
            TenantId = hostTenantId,
            Name = "Host"
        };
    }

    protected virtual ElsaTenant MapAbpTenant(Volo.Abp.TenantManagement.Tenant abpTenant)
    {
        var elsaTenantId = _tenantMapper.ToElsaTenantId(abpTenant.Id);

        return new ElsaTenant
        {
            Id = elsaTenantId,
            TenantId = elsaTenantId,
            Name = abpTenant.Name
        };
    }
}
