namespace BioTrace.Elsa.Abp.Studio;

public class BioTraceElsaAbpStudioOptions
{
    public bool Enabled { get; set; } = true;

    public string PathBase { get; set; } = "/studio";

    public string AuthenticationProvider { get; set; } = "OpenIdConnect";

    public BioTraceElsaAbpStudioBackendOptions Backend { get; set; } = new();

    public BioTraceElsaAbpStudioAuthenticationOptions Authentication { get; set; } = new();

    public BioTraceElsaAbpStudioTenancyOptions Tenancy { get; set; } = new();

    public BioTraceElsaAbpStudioAbpApiOptions AbpApi { get; set; } = new();

    public BioTraceElsaAbpStudioLocalizationOptions Localization { get; set; } = new();
}

public class BioTraceElsaAbpStudioBackendOptions
{
    public string Url { get; set; } = "/elsa/api";
}

public class BioTraceElsaAbpStudioAuthenticationOptions
{
    public BioTraceElsaAbpStudioOpenIdConnectOptions OpenIdConnect { get; set; } = new();
}

public class BioTraceElsaAbpStudioOpenIdConnectOptions
{
    public string? Authority { get; set; }

    public string ClientId { get; set; } = "ElsaStudio";

    public string CallbackPath { get; set; } = "/authentication/login-callback";

    public string SignedOutCallbackPath { get; set; } = "/authentication/logout-callback";

    public string ResponseType { get; set; } = "code";

    public string NameClaimType { get; set; } = "preferred_username";

    public string RoleClaimType { get; set; } = "role";

    public string? AccountManagementUrl { get; set; }

    public List<string> AuthenticationScopes { get; set; } = [];
}

public class BioTraceElsaAbpStudioTenancyOptions
{
    public List<Models.StudioTenantOption> Tenants { get; set; } = [];
}

public class BioTraceElsaAbpStudioAbpApiOptions
{
    public string CurrentUserPath { get; set; } = "identity/users/me";
}

public class BioTraceElsaAbpStudioLocalizationOptions
{
    public string DefaultCulture { get; set; } = "en-US";

    public List<string> SupportedCultures { get; set; } = ["en-US"];
}
