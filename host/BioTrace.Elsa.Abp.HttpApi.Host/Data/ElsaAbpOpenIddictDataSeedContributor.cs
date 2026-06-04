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
        if (await _applicationManager.FindByClientIdAsync(clientId) != null)
        {
            return;
        }

        var rootUrl = section["RootUrl"]?.TrimEnd('/');
        var redirectUris = section.GetSection("RedirectUris").Get<string[]>()
            ?? (rootUrl != null
                ? [$"{rootUrl}/swagger/oauth2-redirect.html"]
                : []);
        var postLogoutUris = section.GetSection("PostLogoutRedirectUris").Get<string[]>()
            ?? (rootUrl != null ? [rootUrl] : []);

        var isSwagger = name.Contains("Swagger", StringComparison.OrdinalIgnoreCase);
        var isStudio = name.Contains("Studio", StringComparison.OrdinalIgnoreCase);

        var application = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            DisplayName = name,
            ClientType = OpenIddictConstants.ClientTypes.Public,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            ApplicationType = OpenIddictConstants.ApplicationTypes.Web
        };

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

        await _applicationManager.CreateAsync(application);
    }
}
