using Elsa.Common.Multitenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.MultiTenancy;

namespace BioTrace.Elsa.Abp.MultiTenancy;

/// <summary>
/// Pushes the resolved ABP tenant into Elsa's <see cref="ITenantAccessor"/> for Elsa API requests.
/// </summary>
public class ElsaAbpMultiTenancyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IElsaAbpTenantMapper _tenantMapper;
    private readonly IOptions<ElsaAbpOptions> _options;
    private readonly ILogger<ElsaAbpMultiTenancyMiddleware> _logger;

    public ElsaAbpMultiTenancyMiddleware(
        RequestDelegate next,
        IElsaAbpTenantMapper tenantMapper,
        IOptions<ElsaAbpOptions> options,
        ILogger<ElsaAbpMultiTenancyMiddleware> logger)
    {
        _next = next;
        _tenantMapper = tenantMapper;
        _options = options;
        _logger = logger;
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
        var tenant = await tenantsProvider.FindAsync(TenantFilter.ById(elsaTenantId), context.RequestAborted);

        if (tenant == null)
        {
            _logger.LogWarning(
                "Elsa tenant '{ElsaTenantId}' was not found for ABP tenant '{AbpTenantId}'.",
                elsaTenantId,
                currentTenant.Id);

            await _next(context);
            return;
        }

        using (tenantAccessor.PushContext(tenant))
        {
            await _next(context);
        }
    }

    protected virtual bool IsElsaRequest(HttpRequest request)
    {
        return request.Path.StartsWithSegments("/elsa", StringComparison.OrdinalIgnoreCase);
    }
}
