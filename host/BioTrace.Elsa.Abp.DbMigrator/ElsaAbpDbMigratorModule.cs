using BioTrace.Elsa.Abp.EntityFrameworkCore;
using BioTrace.Elsa.Abp.MultiTenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Autofac;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.PostgreSql;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.TenantManagement.EntityFrameworkCore;

namespace BioTrace.Elsa.Abp;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(ElsaAbpDbMigratorPostgreSqlModule),
    typeof(ElsaAbpMultiTenancyModule),
    typeof(EntityFrameworkCore.AbpEntityFrameworkCoreModule),
    typeof(AbpEntityFrameworkCorePostgreSqlModule),
    typeof(AbpIdentityEntityFrameworkCoreModule),
    typeof(AbpOpenIddictEntityFrameworkCoreModule),
    typeof(AbpPermissionManagementEntityFrameworkCoreModule),
    typeof(AbpTenantManagementEntityFrameworkCoreModule))]
public class ElsaAbpDbMigratorModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var defaultConnectionString = configuration.GetConnectionString(AbpDbProperties.ConnectionStringName)!;

        Configure<AbpDbConnectionOptions>(options =>
        {
            options.ConnectionStrings.Default = defaultConnectionString;
            options.ConnectionStrings["AbpIdentity"] = defaultConnectionString;
            options.ConnectionStrings["AbpPermissionManagement"] = defaultConnectionString;
            options.ConnectionStrings["AbpOpenIddict"] = defaultConnectionString;
            options.ConnectionStrings["AbpTenantManagement"] = defaultConnectionString;
        });

        Configure<AbpDbContextOptions>(options =>
        {
            options.UseNpgsql(npgsql =>
                npgsql.MigrationsAssembly("BioTrace.Elsa.Abp.HttpApi.Host"));
        });

        Configure<ElsaAbpOptions>(options =>
        {
            var elsaSection = configuration.GetSection("Elsa");
            elsaSection.Bind(options);
            options.RunMigrations = elsaSection.GetValue("RunMigrations", false);
            options.DeferTenantActivationUntilReady = elsaSection.GetValue(
                "DeferTenantActivationUntilReady",
                true);
        });

    }
}
