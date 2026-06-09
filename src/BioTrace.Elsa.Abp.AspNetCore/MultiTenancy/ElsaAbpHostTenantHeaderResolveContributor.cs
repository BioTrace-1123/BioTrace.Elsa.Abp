using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Users;

namespace BioTrace.Elsa.Abp.MultiTenancy;

/// <summary>
/// Lets authenticated Host users impersonate a tenant via the <c>__tenant</c> HTTP header.
/// ABP's <c>CurrentUserTenantResolveContributor</c> runs first and marks <c>Handled</c> for any
/// signed-in user (including Host), which prevents the default header resolver from running.
/// </summary>
public class ElsaAbpHostTenantHeaderResolveContributor : TenantResolveContributorBase, ITransientDependency
{
    public const string ContributorName = "ElsaAbpHostTenantHeader";

    public override string Name => ContributorName;

    public override Task ResolveAsync(ITenantResolveContext context)
    {
        var currentUser = context.ServiceProvider.GetRequiredService<ICurrentUser>();
        if (currentUser.TenantId.HasValue)
        {
            return Task.CompletedTask;
        }

        var httpContext = context.ServiceProvider.GetService<IHttpContextAccessor>()?.HttpContext;
        if (httpContext == null)
        {
            return Task.CompletedTask;
        }

        if (!httpContext.Request.Headers.TryGetValue("__tenant", out var values))
        {
            return Task.CompletedTask;
        }

        var tenantName = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(tenantName))
        {
            return Task.CompletedTask;
        }

        context.TenantIdOrName = tenantName.Trim();
        return Task.CompletedTask;
    }
}
