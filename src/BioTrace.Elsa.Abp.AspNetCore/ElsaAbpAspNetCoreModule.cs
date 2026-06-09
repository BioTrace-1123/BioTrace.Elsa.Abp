using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Http;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Volo.Abp;
using Volo.Abp.AspNetCore;
using Volo.Abp.Modularity;
using Volo.Abp.Security.Claims;
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
        var connectionString = GetElsaConnectionString(configuration, options);
        var enableElsaSwagger = configuration.GetValue(
            "Elsa:EnableElsaSwagger",
            hostEnvironment.IsDevelopment());

        context.Services.AddElsa(elsa =>
        {
            ConfigureElsaCore(elsa, connectionString, options, enableElsaSwagger);
            ConfigureElsaActivities(elsa);
        });
    }

    protected virtual void ConfigureElsaCore(
        IModule elsa,
        string connectionString,
        ElsaAbpOptions options,
        bool enableElsaSwagger)
    {
        ElsaAbpMultiTenancyConfigurator.Configure(elsa, options);

        elsa.UseWorkflowManagement(management =>
        {
            management.UseEntityFrameworkCore(ef =>
            {
                ef.UsePostgreSql(ResolveElsaConnectionString);
                ef.RunMigrations = options.RunMigrations;
            });
        });

        elsa.UseWorkflowRuntime(runtime =>
        {
            runtime.UseEntityFrameworkCore(ef =>
            {
                ef.UsePostgreSql(ResolveElsaConnectionString);
                ef.RunMigrations = options.RunMigrations;
            });
        });

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
