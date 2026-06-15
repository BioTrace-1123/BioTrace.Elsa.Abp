using Microsoft.Extensions.Configuration;
using OpenIddict.Abstractions;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Uow;

namespace BioTrace.Elsa.Abp.Data;

public class ElsaAbpOpenIddictDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IConfiguration _configuration;
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public ElsaAbpOpenIddictDataSeedContributor(
        IConfiguration configuration,
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictScopeManager scopeManager,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _configuration = configuration;
        _applicationManager = applicationManager;
        _scopeManager = scopeManager;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public virtual async Task SeedAsync(DataSeedContext context)
    {
        using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);

        await SeedScopesAsync();
        await SeedApplicationsAsync();

        await uow.CompleteAsync();
    }

    protected virtual async Task SeedScopesAsync()
    {
        if (await _scopeManager.FindByNameAsync("BioTrace_Elsa_Abp") != null)
        {
            return;
        }

        await _scopeManager.CreateAsync(new OpenIddictScopeDescriptor
        {
            Name = "BioTrace_Elsa_Abp",
            DisplayName = "BioTrace Elsa ABP API",
            Resources = { "BioTrace_Elsa_Abp" }
        });
    }

    protected virtual async Task SeedApplicationsAsync()
    {
        var applications = _configuration.GetSection("OpenIddict:Applications").GetChildren();
        foreach (var section in applications)
        {
            await SeedApplicationAsync(section.Key, section);
        }
    }

    protected virtual async Task SeedApplicationAsync(string name, IConfigurationSection section)
    {
        var clientId = section["ClientId"] ?? name;
        var descriptor = CreateApplicationDescriptor(name, section);
        var existing = await _applicationManager.FindByClientIdAsync(clientId);

        if (existing == null)
        {
            await _applicationManager.CreateAsync(descriptor);
            return;
        }

        var current = new OpenIddictApplicationDescriptor();
        await _applicationManager.PopulateAsync(current, existing);

        if (!RequiresApplicationUpdate(current, descriptor))
        {
            return;
        }

        current.RedirectUris.Clear();
        foreach (var uri in descriptor.RedirectUris)
        {
            current.RedirectUris.Add(uri);
        }

        current.PostLogoutRedirectUris.Clear();
        foreach (var uri in descriptor.PostLogoutRedirectUris)
        {
            current.PostLogoutRedirectUris.Add(uri);
        }

        foreach (var permission in descriptor.Permissions)
        {
            current.Permissions.Add(permission);
        }

        await _applicationManager.UpdateAsync(existing, current);
    }

    protected virtual OpenIddictApplicationDescriptor CreateApplicationDescriptor(
        string name,
        IConfigurationSection section)
    {
        var clientId = section["ClientId"] ?? name;
        var rootUrl = section["RootUrl"]?.TrimEnd('/');
        var redirectUris = section.GetSection("RedirectUris").Get<string[]>()
            ?? (rootUrl != null
                ? [$"{rootUrl}/swagger/oauth2-redirect.html"]
                : []);
        var postLogoutUris = section.GetSection("PostLogoutRedirectUris").Get<string[]>()
            ?? (rootUrl != null ? [rootUrl] : []);

        var isSwagger = name.Contains("Swagger", StringComparison.OrdinalIgnoreCase);
        var isStudio = name.Contains("Studio", StringComparison.OrdinalIgnoreCase);
        var isIntegrationTests = name.Contains("IntegrationTests", StringComparison.OrdinalIgnoreCase);

        var application = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            DisplayName = name,
            ClientType = isIntegrationTests
                ? OpenIddictConstants.ClientTypes.Confidential
                : OpenIddictConstants.ClientTypes.Public,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            ApplicationType = OpenIddictConstants.ApplicationTypes.Web
        };

        if (isIntegrationTests)
        {
            var clientSecret = section["ClientSecret"];
            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                throw new InvalidOperationException(
                    $"OpenIddict application '{name}' requires ClientSecret for integration tests.");
            }

            application.ClientSecret = clientSecret;
        }

        foreach (var uri in redirectUris.Where(u => !string.IsNullOrWhiteSpace(u)))
        {
            application.RedirectUris.Add(new Uri(uri, UriKind.Absolute));
        }

        foreach (var uri in postLogoutUris.Where(u => !string.IsNullOrWhiteSpace(u)))
        {
            application.PostLogoutRedirectUris.Add(new Uri(uri, UriKind.Absolute));
        }

        if (isSwagger)
        {
            application.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Authorization);
            application.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
            application.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);
            application.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Code);
            application.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + "BioTrace_Elsa_Abp");
        }
        else if (isStudio)
        {
            application.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Authorization);
            application.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
            application.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.EndSession);
            application.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Revocation);
            application.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);
            application.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);
            application.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Code);
            application.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + "BioTrace_Elsa_Abp");
            application.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + "openid");
            application.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + "profile");
            application.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + "email");
            application.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + "roles");
        }
        else if (isIntegrationTests)
        {
            application.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
            application.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.Password);
            application.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + "BioTrace_Elsa_Abp");
            application.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + "openid");
            application.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + "profile");
            application.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + "roles");
        }

        return application;
    }

    protected virtual bool RequiresApplicationUpdate(
        OpenIddictApplicationDescriptor current,
        OpenIddictApplicationDescriptor desired)
    {
        if (!UriSetsEqual(current.RedirectUris, desired.RedirectUris))
        {
            return true;
        }

        if (!UriSetsEqual(current.PostLogoutRedirectUris, desired.PostLogoutRedirectUris))
        {
            return true;
        }

        return !desired.Permissions.All(permission => current.Permissions.Contains(permission));
    }

    private static bool UriSetsEqual(
        IReadOnlyCollection<Uri> current,
        IReadOnlyCollection<Uri> desired)
    {
        var currentSet = current
            .Select(uri => uri.AbsoluteUri.TrimEnd('/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var desiredSet = desired
            .Select(uri => uri.AbsoluteUri.TrimEnd('/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return currentSet.SetEquals(desiredSet);
    }
}
