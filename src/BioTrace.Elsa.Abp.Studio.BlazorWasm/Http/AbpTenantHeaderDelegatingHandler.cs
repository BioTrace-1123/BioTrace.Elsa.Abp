using BioTrace.Elsa.Abp.Studio.Services;

namespace BioTrace.Elsa.Abp.Studio.Http;

public class AbpTenantHeaderDelegatingHandler : DelegatingHandler
{
    private readonly IAbpStudioTenantContext _tenantContext;

    public AbpTenantHeaderDelegatingHandler(IAbpStudioTenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        await _tenantContext.InitializeAsync(cancellationToken);

        var tenantId = _tenantContext.CurrentTenantId;
        if (tenantId.HasValue)
        {
            request.Headers.TryAddWithoutValidation("__tenant", tenantId.Value.ToString("D"));
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
