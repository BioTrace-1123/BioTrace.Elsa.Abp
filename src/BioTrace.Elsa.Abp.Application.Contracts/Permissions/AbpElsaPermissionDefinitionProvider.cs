using BioTrace.Elsa.Abp.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace BioTrace.Elsa.Abp.Permissions;

public class AbpElsaPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var elsaGroup = context.AddGroup(AbpElsaPermissions.GroupName, L("Permission:Abp.Elsa"));

        elsaGroup.AddPermission(AbpElsaPermissions.Admin, L("Permission:Abp.Elsa.Admin"));
        elsaGroup.AddPermission(AbpElsaPermissions.NotReadOnly, L("Permission:Abp.Elsa.NotReadOnly"));

        var definitions = elsaGroup.AddPermission(
            AbpElsaPermissions.WorkflowDefinitions.Default,
            L("Permission:Abp.Elsa.WorkflowDefinitions"));
        definitions.AddChild(AbpElsaPermissions.WorkflowDefinitions.Read, L("Permission:Abp.Elsa.WorkflowDefinitions.Read"));
        definitions.AddChild(AbpElsaPermissions.WorkflowDefinitions.Write, L("Permission:Abp.Elsa.WorkflowDefinitions.Write"));
        definitions.AddChild(AbpElsaPermissions.WorkflowDefinitions.Publish, L("Permission:Abp.Elsa.WorkflowDefinitions.Publish"));
        definitions.AddChild(AbpElsaPermissions.WorkflowDefinitions.Retract, L("Permission:Abp.Elsa.WorkflowDefinitions.Retract"));
        definitions.AddChild(AbpElsaPermissions.WorkflowDefinitions.Delete, L("Permission:Abp.Elsa.WorkflowDefinitions.Delete"));

        var instances = elsaGroup.AddPermission(
            AbpElsaPermissions.WorkflowInstances.Default,
            L("Permission:Abp.Elsa.WorkflowInstances"));
        instances.AddChild(AbpElsaPermissions.WorkflowInstances.Read, L("Permission:Abp.Elsa.WorkflowInstances.Read"));
        instances.AddChild(AbpElsaPermissions.WorkflowInstances.Write, L("Permission:Abp.Elsa.WorkflowInstances.Write"));
        instances.AddChild(AbpElsaPermissions.WorkflowInstances.Delete, L("Permission:Abp.Elsa.WorkflowInstances.Delete"));
        instances.AddChild(AbpElsaPermissions.WorkflowInstances.Cancel, L("Permission:Abp.Elsa.WorkflowInstances.Cancel"));
        instances.AddChild(AbpElsaPermissions.WorkflowInstances.Execute, L("Permission:Abp.Elsa.WorkflowInstances.Execute"));

        var alterations = elsaGroup.AddPermission(
            AbpElsaPermissions.Alterations.Default,
            L("Permission:Abp.Elsa.Alterations"));
        alterations.AddChild(AbpElsaPermissions.Alterations.Read, L("Permission:Abp.Elsa.Alterations.Read"));
        alterations.AddChild(AbpElsaPermissions.Alterations.Write, L("Permission:Abp.Elsa.Alterations.Write"));

        var tasks = elsaGroup.AddPermission(AbpElsaPermissions.Tasks.Default, L("Permission:Abp.Elsa.Tasks"));
        tasks.AddChild(AbpElsaPermissions.Tasks.Complete, L("Permission:Abp.Elsa.Tasks.Complete"));

        var http = elsaGroup.AddPermission(
            AbpElsaPermissions.HttpEndpoints.Default,
            L("Permission:Abp.Elsa.HttpEndpoints"));
        http.AddChild(AbpElsaPermissions.HttpEndpoints.Invoke, L("Permission:Abp.Elsa.HttpEndpoints.Invoke"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<AbpResource>(name);
    }
}
