namespace BioTrace.Elsa.Abp.Elsa;

public class ElsaAbpCurrentUserDto
{
    public Guid? UserId { get; set; }

    public string? UserName { get; set; }

    public string? Email { get; set; }

    public List<string> Roles { get; set; } = [];

    /// <summary>Elsa FastEndpoints permission strings (e.g. read:workflow-definitions, *).</summary>
    public List<string> Permissions { get; set; } = [];
}
