using Elsa.Common.Multitenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Volo.Abp.MultiTenancy;
using ElsaTenant = Elsa.Common.Multitenancy.Tenant;

namespace BioTrace.Elsa.Abp.MultiTenancy;

/// <summary>
/// Pushes the resolved ABP tenant into Elsa's <see cref="ITenantAccessor"/> for Elsa API requests.
/// </summary>
public class ElsaAbpMultiTenancyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IElsaAbpTenantMapper _tenantMapper;
    private readonly IOptions<ElsaAbpOptions> _options;

    public ElsaAbpMultiTenancyMiddleware(
        RequestDelegate next,
        IElsaAbpTenantMapper tenantMapper,
        IOptions<ElsaAbpOptions> options)
    {
        _next = next;
        _tenantMapper = tenantMapper;
        _options = options;
    }

    public virtual async Task InvokeAsync(
        HttpContext context,
        ITenantAccessor tenantAccessor,
        ITenantsProvider tenantsProvider,
        ICurrentTenant currentTenant)
    {
        if (!_options.Value.EnableMultiTenancy || !IsElsaRequest(context.Request))
        {
            await _next(context);
            return;
        }

        var elsaTenantId = _tenantMapper.ToElsaTenantId(currentTenant.Id);
        var tenant = await tenantsProvider.FindAsync(TenantFilter.ById(elsaTenantId), context.RequestAborted)
            ?? CreateFallbackTenant(elsaTenantId, currentTenant.Name);

        using (tenantAccessor.PushContext(tenant))
        {
            await _next(context);
        }
    }

    protected virtual ElsaTenant CreateFallbackTenant(string elsaTenantId, string? tenantName)
    {
        return new ElsaTenant
        {
            Id = elsaTenantId,
            TenantId = elsaTenantId,
            Name = string.IsNullOrWhiteSpace(tenantName)
                ? (string.IsNullOrEmpty(elsaTenantId) ? "Host" : elsaTenantId)
                : tenantName
        };
    }

    protected virtual bool IsElsaRequest(HttpRequest request)
    {
        return request.Path.StartsWithSegments("/elsa", StringComparison.OrdinalIgnoreCase);
    }
}
