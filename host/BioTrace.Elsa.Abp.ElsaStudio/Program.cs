using BioTrace.Elsa.Abp.ElsaStudio.Components;
using BioTrace.Elsa.Abp.ElsaStudio.Extensions;
using BioTrace.Elsa.Abp.ElsaStudio.Http;
using BioTrace.Elsa.Abp.ElsaStudio.Services;
using Elsa.Studio.Localization.Services;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm.Extensions;
using Elsa.Studio.Authentication.OpenIdConnect.HttpMessageHandlers;
using Elsa.Studio.Contracts;
using Elsa.Studio.Core.BlazorWasm.Extensions;
using Elsa.Studio.Dashboard.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization.BlazorWasm.Extensions;
using Elsa.Studio.Localization.Models;
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

var builder = WebAssemblyHostBuilder.CreateDefault(args);
var configuration = builder.Configuration;

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.RootComponents.Add<UserMenuAppBarRegistration>("body::after");
builder.RootComponents.RegisterCustomElsaStudioElements();

var authProvider = configuration["Authentication:Provider"];
if (string.IsNullOrWhiteSpace(authProvider))
{
    authProvider = "OpenIdConnect";
}

if (!authProvider.Equals("OpenIdConnect", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        $"Unsupported Authentication:Provider value '{authProvider}'. Supported value is 'OpenIdConnect'.");
}

builder.Services.AddOpenIdConnectAuth(options =>
{
    configuration.GetSection("Authentication:OpenIdConnect").Bind(options);
});

builder.Services.AddOptions<RemoteAuthenticationOptions<OidcProviderOptions>>()
    .Configure(options =>
    {
        options.UserOptions.NameClaim =
            configuration["Authentication:OpenIdConnect:NameClaimType"] ?? "preferred_username";
        options.UserOptions.RoleClaim =
            configuration["Authentication:OpenIdConnect:RoleClaimType"] ?? "role";
    });

builder.Services.AddTransient<AbpTenantHeaderDelegatingHandler>();
builder.Services.AddScoped<IAbpStudioTenantContext, AbpStudioTenantContext>();

var backendApiConfig = new BackendApiConfig
{
    ConfigureBackendOptions = options => configuration.GetSection("Backend").Bind(options),
    ConfigureHttpClientBuilder = options =>
    {
        options.AuthenticationHandler = typeof(OidcAuthenticatingApiHttpMessageHandler);
        options.ConfigureHttpClientBuilder = clientBuilder =>
            clientBuilder.AddHttpMessageHandler<AbpTenantHeaderDelegatingHandler>();
    }
};

var localizationConfig = new LocalizationConfig
{
    ConfigureLocalizationOptions = options => configuration.GetSection("Localization").Bind(options)
};

builder.Services.AddCore();
builder.Services.AddShell();
builder.Services.AddRemoteBackend(backendApiConfig);
builder.Services.AddDashboardModule();
builder.Services.AddWorkflowsModule();
builder.Services.AddLocalizationModule(localizationConfig);
builder.Services.AddScoped<ICultureService, AbpElsaStudioCultureService>();

ConfigureAbpStudioIntegration(builder.Services, configuration);

static void ConfigureAbpStudioIntegration(IServiceCollection services, IConfiguration configuration)
{
    var authority = configuration["Authentication:OpenIdConnect:Authority"]?.TrimEnd('/');
    if (string.IsNullOrWhiteSpace(authority))
    {
        throw new InvalidOperationException(
            "Authentication:OpenIdConnect:Authority is required for ABP current-user permission checks.");
    }

    services.AddHttpClient<IElsaAbpStudioPermissionService, ElsaAbpStudioPermissionService>(client =>
        {
            client.BaseAddress = new Uri(authority + "/");
        })
        .AddHttpMessageHandler<AbpTenantHeaderDelegatingHandler>();

    services.AddScoped<ICreateWorkflowDialogComponentProvider, AbpCreateWorkflowDialogComponentProvider>();
}

var app = builder.Build();

var tenantContext = app.Services.GetRequiredService<IAbpStudioTenantContext>();
await tenantContext.InitializeAsync();

// Elsa startup tasks prefetch workflow data; resolve tenant from the signed-in user first.
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
