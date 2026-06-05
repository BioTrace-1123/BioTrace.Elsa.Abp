namespace BioTrace.Elsa.Abp;

public class ElsaAbpOptions
{
    public string ConnectionStringName { get; set; } = ElsaAbpDbProperties.ConnectionStringName;

    public bool RunMigrations { get; set; } = true;

    public bool EnableWorkflowsApi { get; set; } = true;

    /// <summary>Enables a separate FastEndpoints OpenAPI document for Elsa Workflows API (disable in production).</summary>
    public bool EnableElsaSwagger { get; set; }

    public bool EnableHttpActivities { get; set; } = true;

    /// <summary>Maps ABP permissions to Elsa <c>permissions</c> claims on each request.</summary>
    public bool EnablePermissionClaimsBridge { get; set; } = true;

    /// <summary>Claim type used by FastEndpoints (default: permissions).</summary>
    public string PermissionsClaimType { get; set; } = "permissions";

    /// <summary>Role claim type for FastEndpoints (default: role).</summary>
    public string RoleClaimType { get; set; } = "role";

    /// <summary>Disables Elsa endpoint security (development only).</summary>
    public bool DisableElsaEndpointSecurity { get; set; }
}
