using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Http;
using Elsa.Tenants.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Volo.Abp;
using Volo.Abp.AspNetCore;
using Volo.Abp.Modularity;
using Volo.Abp.Security.Claims;
using BioTrace.Elsa.Abp.Data;
using BioTrace.Elsa.Abp.MultiTenancy;
using BioTrace.Elsa.Abp.Security;

namespace BioTrace.Elsa.Abp;

[DependsOn(
    typeof(AbpAspNetCoreModule),
    typeof(AbpApplicationModule),
    typeof(Volo.Abp.PermissionManagement.AbpPermissionManagementDomainModule))]
public class ElsaAbpAspNetCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var hostEnvironment = context.Services.GetHostingEnvironment();

        Configure<ElsaAbpOptions>(options => configuration.GetSection("Elsa").Bind(options));

        Configure<AbpClaimsPrincipalFactoryOptions>(options =>
        {
            options.Contributors.Add<ElsaAbpPermissionClaimsPrincipalContributor>();
        });

        context.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IAuthorizationHandler, AbpElsaNotReadOnlyAuthorizationHandler>());

        ConfigureElsa(context);

        ConfigureElsaSecurity(context, hostEnvironment);

        ConfigureElsaDatabaseMigration(context);
    }

    protected virtual void ConfigureElsaDatabaseMigration(ServiceConfigurationContext context)
    {
        var options = context.Services.ExecutePreConfiguredActions<ElsaAbpOptions>();
        if (!options.RunMigrations)
        {
            return;
        }

        context.Services.AddSingleton<ElsaAbpElsaDatabaseMigrator>();
        context.Services.AddHostedService<ElsaAbpElsaDatabaseMigrationHostedService>();
    }

    protected virtual void ConfigureElsaSecurity(ServiceConfigurationContext context, IHostEnvironment hostEnvironment)
    {
        var options = context.Services.ExecutePreConfiguredActions<ElsaAbpOptions>();

        if (hostEnvironment.IsDevelopment() && options.DisableElsaEndpointSecurity)
        {
            TryDisableElsaEndpointSecurity();
        }
    }

    protected virtual void TryDisableElsaEndpointSecurity()
    {
        var type = Type.GetType("Elsa.Api.Common.Options.EndpointSecurityOptions, Elsa.Api.Common");
        type?.GetMethod("DisableSecurity", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.Invoke(null, null);
    }

    protected virtual void ConfigureElsa(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var hostEnvironment = context.Services.GetHostingEnvironment();
        var options = context.Services.ExecutePreConfiguredActions<ElsaAbpOptions>();
        options.EnableMultiTenancy = options.EnableMultiTenancy
            || configuration.GetValue("Elsa:EnableMultiTenancy", false);
        _ = GetElsaConnectionString(configuration, options);
        var enableElsaSwagger = configuration.GetValue(
            "Elsa:EnableElsaSwagger",
            hostEnvironment.IsDevelopment());

        context.Services.AddElsa(elsa =>
        {
            ConfigureElsaCore(elsa, options, enableElsaSwagger);
            ConfigureElsaActivities(elsa);
        });

        ConfigureElsaMultiTenancyServices(context.Services);
    }

    public override void PostConfigureServices(ServiceConfigurationContext context)
    {
        ConfigureElsaMultiTenancyServices(context.Services);
    }

    protected virtual void ConfigureElsaMultiTenancyServices(IServiceCollection services)
    {
        var options = services.ExecutePreConfiguredActions<ElsaAbpOptions>();
        if (!options.EnableMultiTenancy)
        {
            return;
        }

        services.AddHttpContextAccessor();
        services.RemoveAll<ITenantAccessor>();
        services.AddSingleton<ITenantAccessor, ElsaAbpTenantAccessor>();
        services.RemoveAll<ITenantsProvider>();
        services.AddTransient<ITenantsProvider, ElsaAbpTenantsProvider>();
        services.Configure<TenantsOptions>(tenantOptions => tenantOptions.IsEnabled = true);
    }

    protected virtual void ConfigureElsaCore(
        IModule elsa,
        ElsaAbpOptions options,
        bool enableElsaSwagger)
    {
        ElsaAbpMultiTenancyConfigurator.Configure(elsa, options);

        ConfigureElsaPersistence(elsa, options);

        if (options.EnableWorkflowsApi)
        {
            elsa.UseWorkflowsApi();

            if (enableElsaSwagger)
            {
                ConfigureElsaSwagger(elsa, options);
            }
        }

        if (options.EnableHttpActivities)
        {
            elsa.UseHttp();
        }

        elsa.UseScheduling();
    }

    /// <summary>
    /// Configures Elsa Management/Runtime EF Core persistence (provider-specific).
    /// Override in the host module and reference <c>Elsa.Persistence.EFCore.{Provider}</c>.
    /// </summary>
    protected virtual void ConfigureElsaPersistence(IModule elsa, ElsaAbpOptions options)
    {
        throw new BusinessException(AbpErrorCodes.ElsaPersistenceNotConfigured);
    }

    protected virtual void ConfigureElsaSwagger(IModule elsa, ElsaAbpOptions options)
    {
        elsa.ConfigureElsaSwagger(options);
    }

    protected virtual void ConfigureElsaActivities(IModule elsa)
    {
        elsa.AddActivitiesFrom<AbpApplicationModule>();
    }

    protected virtual string GetElsaConnectionString(IConfiguration configuration, ElsaAbpOptions options)
    {
        var connectionString = configuration.GetConnectionString(options.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new BusinessException(AbpErrorCodes.ElsaConnectionStringNotConfigured)
                .WithData("ConnectionStringName", options.ConnectionStringName);
        }

        return connectionString;
    }

    protected virtual string ResolveElsaConnectionString(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ElsaAbpOptions>>().Value;
        return GetElsaConnectionString(configuration, options);
    }
}
