using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.PostgreSql;
using Volo.Abp.Modularity;
using Volo.Abp.Swashbuckle;

namespace BioTrace.Elsa.Abp;

[DependsOn(
    typeof(AbpHttpApiModule),
    typeof(ElsaAbpAspNetCoreModule),
    typeof(BioTrace.Elsa.Abp.EntityFrameworkCore.AbpEntityFrameworkCoreModule),
    typeof(AbpEntityFrameworkCorePostgreSqlModule),
    typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpSwashbuckleModule))]
public class AbpHttpApiHostModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        Configure<AbpDbContextOptions>(options =>
        {
            options.UseNpgsql();
        });

        ConfigureElsaHost(context, configuration);

        context.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(AbpApplicationModule).Assembly);
        });

        ConfigureSwagger(context);
    }

    protected virtual void ConfigureElsaHost(ServiceConfigurationContext context, IConfiguration configuration)
    {
        Configure<ElsaAbpOptions>(options =>
        {
            configuration.GetSection("Elsa").Bind(options);
            options.RunMigrations = configuration.GetValue("Elsa:RunMigrations", options.RunMigrations);
        });
    }

    protected virtual void ConfigureSwagger(ServiceConfigurationContext context)
    {
        context.Services.AddAbpSwaggerGen(options =>
        {
            options.DocInclusionPredicate((_, _) => true);
            options.CustomSchemaIds(type => type.FullName);
        });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseAbpRequestLocalization();
        app.UseCorrelationId();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseSwagger();
        app.UseAbpSwaggerUI();
        app.UseAbpSerilogEnrichers();
        ConfigureElsaMiddleware(app);
        app.UseConfiguredEndpoints();
    }

    protected virtual void ConfigureElsaMiddleware(IApplicationBuilder app)
    {
        app.UseElsaWorkflows();
    }
}
