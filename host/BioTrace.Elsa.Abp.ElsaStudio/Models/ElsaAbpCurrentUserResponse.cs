namespace BioTrace.Elsa.Abp.ElsaStudio.Models;

public class ElsaAbpCurrentUserResponse
{
    public Guid? UserId { get; set; }

    public string? UserName { get; set; }

    public string? Email { get; set; }

    public Guid? TenantId { get; set; }

    public string? TenantName { get; set; }

    public List<string> Roles { get; set; } = [];

    public List<string> Permissions { get; set; } = [];
}
