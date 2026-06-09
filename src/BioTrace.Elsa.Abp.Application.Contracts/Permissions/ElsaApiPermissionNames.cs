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
        public const string Complete = "tasks:complete";
    }

    /// <summary>
    /// Read permissions required by Elsa Studio designer/descriptor endpoints (Elsa.Workflows.Api 3.5.x).
    /// </summary>
    public static class StudioDescriptors
    {
        public const string ReadCommitStrategies = "read:commit-strategies";
        public const string ReadWorkflowActivationStrategies = "read:workflow-activation-strategies";
        public const string ReadLogPersistenceStrategies = "read:log-persistence-strategies";
        public const string ReadIncidentStrategies = "read:incident-strategies";
        public const string ReadActivityExecution = "read:activity-execution";
        public const string ReadActivityDescriptors = "read:activity-descriptors";
        public const string ReadActivityDescriptorOptions = "read:activity-descriptors-options";
        public const string ReadExpressionDescriptors = "read:expression-descriptors";
        public const string ReadInstalledFeatures = "read:installed-features";
        public const string ReadStorageDrivers = "read:storage-drivers";
        public const string ReadVariableDescriptors = "read:variable-descriptors";
    }

    public static class WorkflowDefinitionActions
    {
        public const string Refresh = "actions:workflow-definitions:refresh";
        public const string Reload = "actions:workflow-definitions:reload";
        public const string Execute = "exec:workflow-definitions";
    }

    public static class Events
    {
        public const string Trigger = "trigger:event";
    }

    public static string[] GetStudioDescriptorReadPermissions()
    {
        return
        [
            StudioDescriptors.ReadCommitStrategies,
            StudioDescriptors.ReadWorkflowActivationStrategies,
            StudioDescriptors.ReadLogPersistenceStrategies,
            StudioDescriptors.ReadIncidentStrategies,
            StudioDescriptors.ReadActivityExecution,
            StudioDescriptors.ReadActivityDescriptors,
            StudioDescriptors.ReadActivityDescriptorOptions,
            StudioDescriptors.ReadExpressionDescriptors,
            StudioDescriptors.ReadInstalledFeatures,
            StudioDescriptors.ReadStorageDrivers,
            StudioDescriptors.ReadVariableDescriptors
        ];
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
            Tasks.Complete,
            ..GetStudioDescriptorReadPermissions(),
            WorkflowDefinitionActions.Refresh,
            WorkflowDefinitionActions.Reload,
            WorkflowDefinitionActions.Execute,
            Events.Trigger
        ];
    }
}
