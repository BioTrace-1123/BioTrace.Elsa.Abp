using BioTrace.Elsa.Abp.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Volo.Abp.AspNetCore;
using Volo.Abp.Data;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Validation.AspNetCore;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Account.Web;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.PostgreSql;
using Volo.Abp.Identity;
using Volo.Abp.Identity.AspNetCore;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement.Identity;
using Volo.Abp.OpenIddict;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Security.Claims;
using Volo.Abp.Swashbuckle;
using Volo.Abp.Timing;

namespace BioTrace.Elsa.Abp;

[DependsOn(
    typeof(AbpHttpApiModule),
    typeof(ElsaAbpAspNetCoreModule),
    typeof(EntityFrameworkCore.AbpEntityFrameworkCoreModule),
    typeof(AbpEntityFrameworkCorePostgreSqlModule),
    typeof(AbpIdentityEntityFrameworkCoreModule),
    typeof(AbpOpenIddictEntityFrameworkCoreModule),
    typeof(AbpPermissionManagementEntityFrameworkCoreModule),
    typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpSwashbuckleModule),
    typeof(AbpAccountWebOpenIddictModule),
    typeof(AbpAccountApplicationModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpIdentityAspNetCoreModule),
    typeof(AbpOpenIddictAspNetCoreModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpPermissionManagementDomainIdentityModule))]
public class AbpHttpApiHostModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var hostEnvironment = context.Services.GetHostingEnvironment();

        PreConfigure<OpenIddictBuilder>(builder =>
        {
            builder.AddValidation(options =>
            {
                options.AddAudiences("BioTrace_Elsa_Abp");
                options.UseLocalServer();
                options.UseAspNetCore();
            });
        });

        if (hostEnvironment.IsDevelopment())
        {
            PreConfigure<AbpOpenIddictAspNetCoreOptions>(options =>
            {
                options.AddDevelopmentEncryptionAndSigningCertificate = true;
            });
        }
        else
        {
            PreConfigure<AbpOpenIddictAspNetCoreOptions>(options =>
            {
                options.AddDevelopmentEncryptionAndSigningCertificate = false;
            });
        }

        if (!hostEnvironment.IsDevelopment())
        {
            var certPath = configuration["AuthServer:CertificatePath"];
            var certPass = configuration["AuthServer:CertificatePassPhrase"];
            if (!string.IsNullOrWhiteSpace(certPath))
            {
                PreConfigure<OpenIddictServerBuilder>(serverBuilder =>
                {
                    serverBuilder.AddProductionEncryptionAndSigningCertificate(certPath, certPass ?? string.Empty);
                });
            }
        }
        PreConfigure<OpenIddictServerBuilder>(serverBuilder =>
        {
            serverBuilder.SetIssuer(new Uri(configuration["AuthServer:Authority"]!.TrimEnd('/')));
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var hostEnvironment = context.Services.GetHostingEnvironment();

        Configure<AbpClockOptions>(options =>
        {
            options.Kind = DateTimeKind.Utc;
        });

        Configure<AbpDbContextOptions>(options =>
        {
            options.UseNpgsql(npgsql =>
                npgsql.MigrationsAssembly(typeof(AbpHttpApiHostModule).Assembly.FullName));
        });

        Configure<AbpDbConnectionOptions>(options =>
        {
            var abpConnectionString = configuration.GetConnectionString(AbpDbProperties.ConnectionStringName)!;
            options.ConnectionStrings.Default = abpConnectionString;
            options.ConnectionStrings["AbpIdentity"] = abpConnectionString;
            options.ConnectionStrings["AbpPermissionManagement"] = abpConnectionString;
            options.ConnectionStrings["AbpOpenIddict"] = abpConnectionString;
        });

        ConfigureElsaHost(context, configuration);

        ConfigureAuthentication(context);
        ConfigureCors(context, configuration);
        ConfigureSwagger(context, configuration);
        Configure<AbpClaimsPrincipalFactoryOptions>(options =>
        {
            options.IsDynamicClaimsEnabled = true;
        });
    }

    protected virtual void ConfigureAuthentication(ServiceConfigurationContext context)
    {
        context.Services.Configure<AuthenticationOptions>(options =>
        {
            options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });
    }

    protected virtual void ConfigureCors(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                var origins = configuration.GetSection("App:CorsOrigins").Get<string[]>() ?? [];
                if (origins.Length == 0)
                {
                    policy.SetIsOriginAllowed(_ => false);
                    return;
                }

                policy
                    .WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

    protected virtual void ConfigureSwagger(ServiceConfigurationContext context, IConfiguration configuration)
    {
        var authority = configuration["AuthServer:Authority"]!.TrimEnd('/');

        context.Services.AddAbpSwaggerGenWithOAuth(
            authority,
            new Dictionary<string, string>
            {
                { "BioTrace_Elsa_Abp", "BioTrace Elsa ABP API" }
            },
            options =>
            {
                options.SwaggerDoc("v1", new() { Title = "BioTrace.Elsa.Abp API", Version = "v1" });
                options.DocInclusionPredicate((_, _) => true);
                options.CustomSchemaIds(type => type.FullName);
            });
    }

    protected virtual void ConfigureElsaHost(ServiceConfigurationContext context, IConfiguration configuration)
    {
        Configure<ElsaAbpOptions>(options =>
        {
            configuration.GetSection("Elsa").Bind(options);
            options.RunMigrations = configuration.GetValue("Elsa:RunMigrations", options.RunMigrations);
            options.DisableElsaEndpointSecurity = configuration.GetValue(
                "Elsa:DisableElsaEndpointSecurity",
                options.DisableElsaEndpointSecurity);
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
        app.UseAbpOpenIddictValidation();
        app.UseAuthorization();
        app.UseSwagger();
        app.UseAbpSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "BioTrace.Elsa.Abp API");
            var configuration = context.ServiceProvider.GetRequiredService<IConfiguration>();
            options.OAuthClientId(configuration["AuthServer:SwaggerClientId"]);
            options.OAuthScopes("BioTrace_Elsa_Abp");
        });
        app.UseAbpSerilogEnrichers();
        ConfigureElsaMiddleware(app);
        app.UseConfiguredEndpoints();
    }

    protected virtual void ConfigureElsaMiddleware(IApplicationBuilder app)
    {
        app.UseElsaWorkflows();
    }
}
