using Elsa.Common.Multitenancy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using ElsaTenant = Elsa.Common.Multitenancy.Tenant;

namespace BioTrace.Elsa.Abp.MultiTenancy;

/// <summary>
/// Decorates <see cref="ElsaAbpTenantsProvider"/> and returns Host-only tenants when ABP tenant storage is not ready.
/// </summary>
public class ElsaAbpDeferringTenantsProvider : ITenantsProvider, ITransientDependency
{
    private readonly ElsaAbpTenantsProvider _inner;
    private readonly IElsaAbpTenantMapper _tenantMapper;
    private readonly IOptions<ElsaAbpOptions> _options;
    private readonly ILogger<ElsaAbpDeferringTenantsProvider> _logger;
    private volatile bool _isReady;

    public ElsaAbpDeferringTenantsProvider(
        ElsaAbpTenantsProvider inner,
        IElsaAbpTenantMapper tenantMapper,
        IOptions<ElsaAbpOptions> options,
        ILogger<ElsaAbpDeferringTenantsProvider>? logger = null)
    {
        _inner = inner;
        _tenantMapper = tenantMapper;
        _options = options;
        _logger = logger ?? NullLogger<ElsaAbpDeferringTenantsProvider>.Instance;
    }

    public virtual async Task<IEnumerable<ElsaTenant>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Value.DeferTenantActivationUntilReady)
        {
            return await _inner.ListAsync(cancellationToken);
        }

        if (_isReady)
        {
            return await _inner.ListAsync(cancellationToken);
        }

        try
        {
            var tenants = await _inner.ListAsync(cancellationToken);
            _isReady = true;
            return tenants;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "ABP tenant storage is not ready; deferring Elsa tenant activation to Host-only until migration completes.");
            return [CreateHostTenant()];
        }
    }

    public virtual async Task<ElsaTenant?> FindAsync(TenantFilter filter, CancellationToken cancellationToken = default)
    {
        if (!_options.Value.DeferTenantActivationUntilReady)
        {
            return await _inner.FindAsync(filter, cancellationToken);
        }

        if (string.IsNullOrEmpty(filter.Id))
        {
            return CreateHostTenant();
        }

        if (_isReady)
        {
            return await _inner.FindAsync(filter, cancellationToken);
        }

        try
        {
            var tenant = await _inner.FindAsync(filter, cancellationToken);
            _isReady = true;
            return tenant;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "ABP tenant storage is not ready; deferring Elsa tenant lookup for {TenantId}.",
                filter.Id);
            return null;
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
}
