namespace BioTrace.Elsa.Abp.MultiTenancy;

/// <summary>Stable names for host demo and integration tests.</summary>
public static class ElsaAbpMultiTenancySeedData
{
    public const string TenantAName = "tenant-a";
    public const string TenantBName = "tenant-b";

    /// <summary>CJK tenant name for Studio WASM __tenant header E2E.</summary>
    public const string TenantCjkName = "宝通";

    public const string DemoCjkTenantWorkflow = "DemoCjkTenantWorkflow";

    public const string TenantAAdminUserName = "tenant-a-admin";
    public const string TenantBAdminUserName = "tenant-b-admin";

    public const string TenantADesignerUserName = "tenant-a-designer";
    public const string TenantBDesignerUserName = "tenant-b-designer";
}
