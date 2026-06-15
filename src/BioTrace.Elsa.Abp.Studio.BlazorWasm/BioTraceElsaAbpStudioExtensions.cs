using BioTrace.Elsa.Abp.Studio.Components;
using BioTrace.Elsa.Abp.Studio.Extensions;
using BioTrace.Elsa.Abp.Studio.Http;
using BioTrace.Elsa.Abp.Studio.Services;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm.Extensions;
using Elsa.Studio.Authentication.OpenIdConnect.HttpMessageHandlers;
using Elsa.Studio.Contracts;
using Elsa.Studio.Core.BlazorWasm.Extensions;
using Elsa.Studio.Dashboard.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization.BlazorWasm.Extensions;
using Elsa.Studio.Localization.Models;
using Elsa.Studio.Localization.Services;
using Elsa.Studio.Models;
using Elsa.Studio.Shell;
using Elsa.Studio.Shell.Extensions;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Designer.Extensions;
using Elsa.Studio.Workflows.Extensions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BioTrace.Elsa.Abp.Studio;

public static class BioTraceElsaAbpStudioExtensions
{
    public static WebAssemblyHostBuilder AddBioTraceElsaAbpStudio(
        this WebAssemblyHostBuilder builder,
        Action<BioTraceElsaAbpStudioOptions>? configure = null)
    {
        var configuration = builder.Configuration;
        var options = new BioTraceElsaAbpStudioOptions();
        configuration.GetSection("ElsaStudio").Bind(options);
        configure?.Invoke(options);

        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");
        builder.RootComponents.Add<UserMenuAppBarRegistration>("body::after");
        builder.RootComponents.RegisterCustomElsaStudioElements();

        if (!options.AuthenticationProvider.Equals("OpenIdConnect", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unsupported ElsaStudio:AuthenticationProvider value '{options.AuthenticationProvider}'. Supported value is 'OpenIdConnect'.");
        }

        builder.Services.AddOpenIdConnectAuth(oidcOptions =>
        {
            configuration.GetSection("ElsaStudio:Authentication:OpenIdConnect").Bind(oidcOptions);
        });

        builder.Services.AddOptions<RemoteAuthenticationOptions<OidcProviderOptions>>()
            .Configure(oidcOptions =>
            {
                oidcOptions.UserOptions.NameClaim =
                    configuration["ElsaStudio:Authentication:OpenIdConnect:NameClaimType"] ?? "preferred_username";
                oidcOptions.UserOptions.RoleClaim =
                    configuration["ElsaStudio:Authentication:OpenIdConnect:RoleClaimType"] ?? "role";
            });

        builder.Services.AddTransient<AbpTenantHeaderDelegatingHandler>();
        builder.Services.AddScoped<IAbpStudioTenantContext, AbpStudioTenantContext>();
        builder.Services.Configure<BioTraceElsaAbpStudioOptions>(configuration.GetSection("ElsaStudio"));
        RegisterStudioTenantDirectory(builder, configuration);

        var backendApiConfig = new BackendApiConfig
        {
            ConfigureBackendOptions = backendOptions =>
            {
                configuration.GetSection("ElsaStudio:Backend").Bind(backendOptions);
                backendOptions.Url = new Uri(ResolveAbsoluteBackendUrl(
                    backendOptions.Url?.ToString(),
                    configuration,
                    builder.HostEnvironment.BaseAddress));
            },
            ConfigureHttpClientBuilder = httpOptions =>
            {
                httpOptions.AuthenticationHandler = typeof(OidcAuthenticatingApiHttpMessageHandler);
                httpOptions.ConfigureHttpClientBuilder = clientBuilder =>
                    clientBuilder.AddHttpMessageHandler<AbpTenantHeaderDelegatingHandler>();
            }
        };

        var localizationConfig = new LocalizationConfig
        {
            ConfigureLocalizationOptions = localizationOptions =>
                configuration.GetSection("ElsaStudio:Localization").Bind(localizationOptions)
        };

        builder.Services.AddCore();
        builder.Services.AddShell();
        builder.Services.AddRemoteBackend(backendApiConfig);
        builder.Services.AddDashboardModule();
        builder.Services.AddWorkflowsModule();
        builder.Services.AddLocalizationModule(localizationConfig);
        builder.Services.AddScoped<ICultureService, AbpElsaStudioCultureService>();

        ConfigureAbpStudioIntegration(builder.Services, configuration);

        return builder;
    }

    public static async Task RunBioTraceElsaAbpStudioAsync(this WebAssemblyHost app)
    {
        var tenantContext = app.Services.GetRequiredService<IAbpStudioTenantContext>();
        await tenantContext.InitializeAsync();

        var authState = await app.Services
            .GetRequiredService<AuthenticationStateProvider>()
            .GetAuthenticationStateAsync();
        if (authState.User.Identity?.IsAuthenticated == true)
        {
            try
            {
                await app.Services
                    .GetRequiredService<IElsaAbpStudioPermissionService>()
                    .GetPermissionsAsync();
            }
            catch (HttpRequestException)
            {
                // Host may be unavailable during local startup; tenant context will sync on first API call.
            }
        }

        await app.UseAbpElsaStudioLocalizationAsync();

        var startupTaskRunner = app.Services.GetRequiredService<IStartupTaskRunner>();
        await startupTaskRunner.RunStartupTasksAsync();

        await app.RunAsync();
    }

    private static void ConfigureAbpStudioIntegration(IServiceCollection services, IConfiguration configuration)
    {
        var authority = configuration["ElsaStudio:Authentication:OpenIdConnect:Authority"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(authority))
        {
            throw new InvalidOperationException(
                "ElsaStudio:Authentication:OpenIdConnect:Authority is required for ABP current-user permission checks.");
        }

        services.AddHttpClient<IElsaAbpStudioPermissionService, ElsaAbpStudioPermissionService>(client =>
            {
                client.BaseAddress = new Uri(authority + "/");
            })
            .AddHttpMessageHandler<AbpTenantHeaderDelegatingHandler>();

        services.AddScoped<ICreateWorkflowDialogComponentProvider, AbpCreateWorkflowDialogComponentProvider>();
    }

    private static void RegisterStudioTenantDirectory(WebAssemblyHostBuilder builder, IConfiguration configuration)
    {
        var authority = configuration["ElsaStudio:Authentication:OpenIdConnect:Authority"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(authority))
        {
            builder.Services.AddScoped<IStudioTenantDirectory, ConfigurationStudioTenantDirectory>();
            return;
        }

        builder.Services.AddHttpClient<AbpApiStudioTenantDirectory>(client =>
            {
                client.BaseAddress = new Uri(authority + "/");
            });

        builder.Services.AddScoped<ConfigurationStudioTenantDirectory>();
        builder.Services.AddScoped<IStudioTenantDirectory, StudioTenantDirectory>();
    }

    internal static string ResolveAbsoluteBackendUrl(
        string? configuredUrl,
        IConfiguration configuration,
        string hostBaseAddress)
    {
        var url = string.IsNullOrWhiteSpace(configuredUrl) ? "/elsa/api" : configuredUrl.Trim();

        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute) && absolute.IsAbsoluteUri)
        {
            return absolute.ToString().TrimEnd('/');
        }

        var authority = configuration["ElsaStudio:Authentication:OpenIdConnect:Authority"]?.TrimEnd('/');
        string origin;
        if (!string.IsNullOrWhiteSpace(authority) && Uri.TryCreate(authority, UriKind.Absolute, out var authorityUri))
        {
            origin = authorityUri.GetLeftPart(UriPartial.Authority);
        }
        else if (Uri.TryCreate(hostBaseAddress, UriKind.Absolute, out var baseUri))
        {
            origin = baseUri.GetLeftPart(UriPartial.Authority);
        }
        else
        {
            throw new InvalidOperationException(
                "ElsaStudio:Backend:Url is relative but no absolute Authority or host base address is available.");
        }

        var path = url.StartsWith('/') ? url : "/" + url;
        return (origin + path).TrimEnd('/');
    }
}
