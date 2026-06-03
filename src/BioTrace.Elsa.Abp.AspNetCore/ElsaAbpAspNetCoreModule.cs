using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Features.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.AspNetCore;
using Volo.Abp.Modularity;

namespace BioTrace.Elsa.Abp;

[DependsOn(
    typeof(AbpAspNetCoreModule),
    typeof(AbpApplicationModule))]
public class ElsaAbpAspNetCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<ElsaAbpOptions>(options => configuration.GetSection("Elsa").Bind(options));
        ConfigureElsa(context);
    }

    protected virtual void ConfigureElsa(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var options = context.Services.ExecutePreConfiguredActions<ElsaAbpOptions>();
        var connectionString = GetElsaConnectionString(configuration, options);

        context.Services.AddElsa(elsa =>
        {
            ConfigureElsaCore(elsa, connectionString, options);
            ConfigureElsaActivities(elsa);
        });
    }

    protected virtual void ConfigureElsaCore(
        IModule elsa,
        string connectionString,
        ElsaAbpOptions options)
    {
        elsa.UseWorkflowManagement(management =>
        {
            management.UseEntityFrameworkCore(ef =>
            {
                ef.UsePostgreSql(connectionString);
                ef.RunMigrations = options.RunMigrations;
            });
        });

        elsa.UseWorkflowRuntime(runtime =>
        {
            runtime.UseEntityFrameworkCore(ef =>
            {
                ef.UsePostgreSql(connectionString);
                ef.RunMigrations = options.RunMigrations;
            });
        });

        if (options.EnableWorkflowsApi)
        {
            elsa.UseWorkflowsApi();
        }

        if (options.EnableHttpActivities)
        {
            elsa.UseHttp();
        }
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
}
