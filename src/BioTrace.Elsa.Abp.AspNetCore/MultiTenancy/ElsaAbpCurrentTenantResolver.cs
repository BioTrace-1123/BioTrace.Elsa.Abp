using Elsa.Common.Multitenancy;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;

namespace BioTrace.Elsa.Abp.MultiTenancy;

public class ElsaAbpCurrentTenantResolver : TenantResolverBase, ITransientDependency
{
    private readonly ICurrentTenant _currentTenant;
    private readonly IElsaAbpTenantMapper _tenantMapper;

    public ElsaAbpCurrentTenantResolver(
        ICurrentTenant currentTenant,
        IElsaAbpTenantMapper tenantMapper)
    {
        _currentTenant = currentTenant;
        _tenantMapper = tenantMapper;
    }

    protected override Task<TenantResolverResult> ResolveAsync(TenantResolverContext context)
    {
        var elsaTenantId = _tenantMapper.ToElsaTenantId(_currentTenant.Id);
        return Task.FromResult(AutoResolve(elsaTenantId));
    }
}
