using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using BioTrace.Elsa.Abp.MultiTenancy;
using Volo.Abp.Modularity;

namespace BioTrace.Elsa.Abp;

/// <summary>
/// Demonstration host: wires Elsa Management/Runtime to PostgreSQL via Elsa.Persistence.EFCore.PostgreSql.
/// </summary>
[DependsOn(typeof(ElsaAbpMultiTenancyModule))]
public class ElsaAbpHostPostgreSqlModule : ElsaAbpAspNetCoreModule
{
    protected override void ConfigureElsaPersistence(IModule elsa, ElsaAbpOptions options)
    {
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
    }
}
