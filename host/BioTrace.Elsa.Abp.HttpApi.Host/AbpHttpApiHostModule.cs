using BioTrace.Elsa.Abp.EntityFrameworkCore;
using BioTrace.Elsa.Abp.MultiTenancy;
using BioTrace.Elsa.Abp.Studio;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Volo.Abp.AspNetCore;
using Volo.Abp.Data;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenIddict.Validation.AspNetCore;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Account.Web;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.Libs;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Basic;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Basic.Bundling;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.PostgreSql;
using Volo.Abp.Identity;
using Volo.Abp.Identity.AspNetCore;
using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement.Identity;
using Volo.Abp.OpenIddict;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Security.Claims;
using Volo.Abp.Swashbuckle;
using Volo.Abp.Timing;
using Volo.Abp.TenantManagement;
using Volo.Abp.TenantManagement.EntityFrameworkCore;
using Volo.Abp.UI.Navigation.Urls;

namespace BioTrace.Elsa.Abp;

[DependsOn(
    typeof(AbpHttpApiModule),
    typeof(ElsaAbpAspNetCoreModule),
    typeof(ElsaAbpMultiTenancyModule),
    typeof(EntityFrameworkCore.AbpEntityFrameworkCoreModule),
    typeof(AbpEntityFrameworkCorePostgreSqlModule),
    typeof(AbpIdentityEntityFrameworkCoreModule),
    typeof(AbpOpenIddictEntityFrameworkCoreModule),
    typeof(AbpPermissionManagementEntityFrameworkCoreModule),
    typeof(AbpTenantManagementEntityFrameworkCoreModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpSwashbuckleModule),
    typeof(AbpAspNetCoreMvcUiBasicThemeModule),
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

            if (configuration.GetValue("AuthServer:AllowPasswordGrantForIntegrationTests", false))
            {
                serverBuilder.AllowPasswordFlow();
            }
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
            var defaultConnectionString = configuration.GetConnectionString(AbpDbProperties.ConnectionStringName)!;
            options.ConnectionStrings.Default = defaultConnectionString;
            options.ConnectionStrings["AbpIdentity"] = defaultConnectionString;
            options.ConnectionStrings["AbpPermissionManagement"] = defaultConnectionString;
            options.ConnectionStrings["AbpOpenIddict"] = defaultConnectionString;
            options.ConnectionStrings["AbpTenantManagement"] = defaultConnectionString;
        });

        Configure<AbpMultiTenancyOptions>(options =>
        {
            options.IsEnabled = MultiTenancyConsts.IsEnabled;
        });

        Configure<AbpMvcLibsOptions>(options =>
        {
            options.CheckLibs = false;
        });

        ConfigureElsaHost(context, configuration);

        ConfigureAuthentication(context);
        ConfigureBundles();
        ConfigureUrls(configuration);
        ConfigureCors(context, configuration);
        ConfigureSwagger(context, configuration);
        Configure<AbpClaimsPrincipalFactoryOptions>(options =>
        {
            options.IsDynamicClaimsEnabled = true;
        });

        context.Services.AddBioTraceElsaAbpStudioHost(configuration);
    }

    protected virtual void ConfigureAuthentication(ServiceConfigurationContext context)
    {
        context.Services.ForwardIdentityAuthenticationForBearer(
            OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

        context.Services.ConfigureApplicationCookie(options =>
        {
            options.Events.OnRedirectToLogin = redirectContext =>
            {
                if (ShouldReturn401ForChallenge(redirectContext.Request))
                {
                    redirectContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                redirectContext.Response.Redirect(redirectContext.RedirectUri);
                return Task.CompletedTask;
            };

            options.Events.OnRedirectToAccessDenied = redirectContext =>
            {
                if (ShouldReturn401ForChallenge(redirectContext.Request))
                {
                    redirectContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }

                redirectContext.Response.Redirect(redirectContext.RedirectUri);
                return Task.CompletedTask;
            };
        });
    }

    protected virtual bool ShouldReturn401ForChallenge(HttpRequest request)
    {
        if (request.Path.StartsWithSegments("/api")
            || request.Path.StartsWithSegments("/elsa/api"))
        {
            return true;
        }

        return request.Headers.Accept.Any(value =>
            value?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);
    }

    protected virtual void ConfigureBundles()
    {
        Configure<AbpBundlingOptions>(options =>
        {
            options.StyleBundles.Configure(
                BasicThemeBundles.Styles.Global,
                bundle => bundle.AddFiles("/global-styles.css"));
        });
    }

    protected virtual void ConfigureUrls(IConfiguration configuration)
    {
        Configure<AppUrlOptions>(options =>
        {
            options.Applications["MVC"].RootUrl = configuration["App:SelfUrl"];
            var allowed = configuration["App:RedirectAllowedUrls"];
            if (!string.IsNullOrWhiteSpace(allowed))
            {
                options.RedirectAllowedUrls.AddRange(
                    allowed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }
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
                options.DocInclusionPredicate((docName, apiDesc) =>
                    string.Equals(docName, "v1", StringComparison.Ordinal)
                    && apiDesc.ActionDescriptor is ControllerActionDescriptor);
                options.CustomSchemaIds(type => type.FullName);
            });
    }

    protected virtual void ConfigureElsaHost(ServiceConfigurationContext context, IConfiguration configuration)
    {
        var hostEnvironment = context.Services.GetHostingEnvironment();

        Configure<ElsaAbpOptions>(options =>
        {
            configuration.GetSection("Elsa").Bind(options);
            options.RunMigrations = configuration.GetValue("Elsa:RunMigrations", options.RunMigrations);
            options.EnableMultiTenancy = configuration.GetValue(
                "Elsa:EnableMultiTenancy",
                MultiTenancyConsts.IsEnabled);
            options.DisableElsaEndpointSecurity = configuration.GetValue(
                "Elsa:DisableElsaEndpointSecurity",
                options.DisableElsaEndpointSecurity);
            options.EnableElsaSwagger = configuration.GetValue(
                "Elsa:EnableElsaSwagger",
                hostEnvironment.IsDevelopment());
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
        app.UseBioTraceElsaAbpStudioHost();
        app.UseStaticFiles();
        app.MapAbpStaticAssets();
        app.UseRouting();
        app.UseCors();
        app.UseAuthentication();
        app.UseAbpOpenIddictValidation();
        app.UseMultiTenancy();
        app.UseAuthorization();
        var elsaOptions = context.ServiceProvider.GetRequiredService<IOptions<ElsaAbpOptions>>().Value;

        // Elsa OpenAPI must run before Swashbuckle: both default to /swagger/{name}/swagger.json and
        // Swashbuckle would 404 unknown document names (e.g. "elsa") before FastEndpoints runs.
        ConfigureElsaMiddleware(app);

        app.UseSwagger();
        app.UseAbpSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "BioTrace.Elsa.Abp API");
            if (elsaOptions.EnableElsaSwagger)
            {
                options.SwaggerEndpoint(ElsaAbpSwaggerExtensions.OpenApiJsonPath, "Elsa Workflows API");
            }

            var configuration = context.ServiceProvider.GetRequiredService<IConfiguration>();
            options.OAuthClientId(configuration["AuthServer:SwaggerClientId"]);
            options.OAuthScopes("BioTrace_Elsa_Abp");
        });
        app.UseAbpSerilogEnrichers();
        app.UseBioTraceElsaAbpStudioFallback();
        app.UseConfiguredEndpoints();
    }

    protected virtual void ConfigureElsaMiddleware(IApplicationBuilder app)
    {
        app.UseElsaAbpMultiTenancy();
        app.UseElsaWorkflows();
    }
}
