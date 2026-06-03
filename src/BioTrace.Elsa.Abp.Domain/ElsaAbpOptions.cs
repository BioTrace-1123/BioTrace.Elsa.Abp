namespace BioTrace.Elsa.Abp;

public class ElsaAbpOptions
{
    public string ConnectionStringName { get; set; } = ElsaAbpDbProperties.ConnectionStringName;

    public bool RunMigrations { get; set; } = true;

    public bool EnableWorkflowsApi { get; set; } = true;

    public bool EnableHttpActivities { get; set; } = true;
}
