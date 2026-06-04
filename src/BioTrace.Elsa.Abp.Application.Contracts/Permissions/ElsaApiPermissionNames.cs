namespace BioTrace.Elsa.Abp.Permissions;

/// <summary>
/// Elsa Workflows API permission strings (FastEndpoints <c>permissions</c> claim values).
/// Must stay aligned with Elsa.Workflows.Api 3.5.x endpoint <c>ConfigurePermissions</c> calls.
/// </summary>
public static class ElsaApiPermissionNames
{
    public const string Wildcard = "*";

    public static class WorkflowDefinitions
    {
        public const string Read = "read:workflow-definitions";
        public const string Write = "write:workflow-definitions";
        public const string Publish = "publish:workflow-definitions";
        public const string Retract = "retract:workflow-definitions";
        public const string Delete = "delete:workflow-definitions";
    }

    public static class WorkflowInstances
    {
        public const string Read = "read:workflow-instances";
        public const string Write = "write:workflow-instances";
        public const string Delete = "delete:workflow-instances";
        public const string Cancel = "cancel:workflow-instances";
        public const string Execute = "execute:workflow-instances";
    }

    public static class Alterations
    {
        public const string Read = "read:alterations";
        public const string Write = "write:alterations";
    }

    public static class Tasks
    {
        public const string Complete = "complete:tasks";
    }

    public static string[] GetAll()
    {
        return
        [
            Wildcard,
            WorkflowDefinitions.Read,
            WorkflowDefinitions.Write,
            WorkflowDefinitions.Publish,
            WorkflowDefinitions.Retract,
            WorkflowDefinitions.Delete,
            WorkflowInstances.Read,
            WorkflowInstances.Write,
            WorkflowInstances.Delete,
            WorkflowInstances.Cancel,
            WorkflowInstances.Execute,
            Alterations.Read,
            Alterations.Write,
            Tasks.Complete
        ];
    }
}
