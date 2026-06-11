using Elsa.Common.Multitenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using ElsaTenant = Elsa.Common.Multitenancy.Tenant;

namespace BioTrace.Elsa.Abp.MultiTenancy;

/// <summary>
/// Bridges ABP <see cref="ICurrentTenant"/> to Elsa <see cref="ITenantAccessor"/> for EF tenant filters and saves.
/// </summary>
public class ElsaAbpTenantAccessor : ITenantAccessor, ISingletonDependency
{
    private static readonly AsyncLocal<ElsaTenant?> CurrentTenantField = new();

    private readonly IHttpContextAccessor _httpContextAccessor;

    public ElsaAbpTenantAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public virtual ElsaTenant? Tenant => CurrentTenantField.Value ?? ResolveFromAbp();

    public virtual string TenantId => Tenant?.TenantId ?? string.Empty;

    public virtual IDisposable PushContext(ElsaTenant? tenant)
    {
        var previous = CurrentTenantField.Value;
        CurrentTenantField.Value = tenant;
        return new RestoreContext(previous);
    }

    protected virtual ElsaTenant? ResolveFromAbp()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return null;
        }

        var options = httpContext.RequestServices.GetRequiredService<IOptions<ElsaAbpOptions>>().Value;
        if (!options.EnableMultiTenancy)
        {
            return null;
        }

        var currentTenant = httpContext.RequestServices.GetRequiredService<ICurrentTenant>();
        var mapper = httpContext.RequestServices.GetRequiredService<IElsaAbpTenantMapper>();
        var elsaTenantId = mapper.ToElsaTenantId(currentTenant.Id);

        return CreateTenant(elsaTenantId, currentTenant.Name);
    }

    protected virtual ElsaTenant CreateTenant(string elsaTenantId, string? name)
    {
        return new ElsaTenant
        {
            Id = elsaTenantId,
            TenantId = elsaTenantId,
            Name = string.IsNullOrWhiteSpace(name)
                ? (string.IsNullOrEmpty(elsaTenantId) ? "Host" : elsaTenantId)
                : name
        };
    }

    private sealed class RestoreContext : IDisposable
    {
        private readonly ElsaTenant? _previous;

        public RestoreContext(ElsaTenant? previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            CurrentTenantField.Value = _previous;
        }
    }
}
