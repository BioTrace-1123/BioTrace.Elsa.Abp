using BioTrace.Elsa.Abp.Permissions;
using Volo.Abp.DependencyInjection;

namespace BioTrace.Elsa.Abp.Security;

public class ElsaAbpPermissionMapper : IElsaAbpPermissionMapper, ITransientDependency
{
    private static readonly string[] StudioDescriptorReadPermissions =
        ElsaApiPermissionNames.GetStudioDescriptorReadPermissions();

    private static readonly string[] WorkflowDefinitionWritePermissions =
    [
        ElsaApiPermissionNames.WorkflowDefinitions.Write,
        ElsaApiPermissionNames.WorkflowDefinitionActions.Refresh,
        ElsaApiPermissionNames.WorkflowDefinitionActions.Reload,
        ElsaApiPermissionNames.WorkflowDefinitionActions.Execute
    ];

    private static readonly IReadOnlyDictionary<string, string[]> AbpToElsaMap = BuildMap();

    public virtual IReadOnlyList<string> MapToElsaPermissions(IEnumerable<string> grantedAbpPermissionNames)
    {
        var granted = new HashSet<string>(grantedAbpPermissionNames, StringComparer.OrdinalIgnoreCase);
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (granted.Contains(AbpElsaPermissions.Admin))
        {
            result.Add(ElsaApiPermissionNames.Wildcard);
            return result.ToList();
        }

        foreach (var (abpPermission, elsaPermissions) in AbpToElsaMap)
        {
            if (granted.Contains(abpPermission))
            {
                foreach (var elsa in elsaPermissions)
                {
                    result.Add(elsa);
                }
            }
        }

        return result.ToList();
    }

    protected virtual IReadOnlyDictionary<string, string[]> GetAbpToElsaMap() => AbpToElsaMap;

    private static IReadOnlyDictionary<string, string[]> BuildMap()
    {
        return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [AbpElsaPermissions.WorkflowDefinitions.Default] =
            [
                ElsaApiPermissionNames.WorkflowDefinitions.Read,
                ..StudioDescriptorReadPermissions,
                ..WorkflowDefinitionWritePermissions,
                ElsaApiPermissionNames.WorkflowDefinitions.Publish,
                ElsaApiPermissionNames.WorkflowDefinitions.Retract,
                ElsaApiPermissionNames.WorkflowDefinitions.Delete
            ],
            [AbpElsaPermissions.WorkflowDefinitions.Read] =
            [
                ElsaApiPermissionNames.WorkflowDefinitions.Read,
                ..StudioDescriptorReadPermissions
            ],
            [AbpElsaPermissions.WorkflowDefinitions.Write] = WorkflowDefinitionWritePermissions,
            [AbpElsaPermissions.WorkflowDefinitions.Publish] = [ElsaApiPermissionNames.WorkflowDefinitions.Publish],
            [AbpElsaPermissions.WorkflowDefinitions.Retract] = [ElsaApiPermissionNames.WorkflowDefinitions.Retract],
            [AbpElsaPermissions.WorkflowDefinitions.Delete] = [ElsaApiPermissionNames.WorkflowDefinitions.Delete],
            [AbpElsaPermissions.WorkflowInstances.Default] =
            [
                ElsaApiPermissionNames.WorkflowInstances.Read,
                ElsaApiPermissionNames.WorkflowInstances.Write,
                ElsaApiPermissionNames.WorkflowInstances.Delete,
                ElsaApiPermissionNames.WorkflowInstances.Cancel,
                ElsaApiPermissionNames.WorkflowInstances.Execute
            ],
            [AbpElsaPermissions.WorkflowInstances.Read] = [ElsaApiPermissionNames.WorkflowInstances.Read],
            [AbpElsaPermissions.WorkflowInstances.Write] = [ElsaApiPermissionNames.WorkflowInstances.Write],
            [AbpElsaPermissions.WorkflowInstances.Delete] = [ElsaApiPermissionNames.WorkflowInstances.Delete],
            [AbpElsaPermissions.WorkflowInstances.Cancel] = [ElsaApiPermissionNames.WorkflowInstances.Cancel],
            [AbpElsaPermissions.WorkflowInstances.Execute] =
            [
                ElsaApiPermissionNames.WorkflowInstances.Execute,
                ElsaApiPermissionNames.Events.Trigger
            ],
            [AbpElsaPermissions.Alterations.Default] =
            [
                ElsaApiPermissionNames.Alterations.Read,
                ElsaApiPermissionNames.Alterations.Write
            ],
            [AbpElsaPermissions.Alterations.Read] = [ElsaApiPermissionNames.Alterations.Read],
            [AbpElsaPermissions.Alterations.Write] = [ElsaApiPermissionNames.Alterations.Write],
            [AbpElsaPermissions.Tasks.Complete] = [ElsaApiPermissionNames.Tasks.Complete],
            [AbpElsaPermissions.Tasks.Default] = [ElsaApiPermissionNames.Tasks.Complete]
        };
    }
}
