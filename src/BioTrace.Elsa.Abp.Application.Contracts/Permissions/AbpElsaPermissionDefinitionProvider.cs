using BioTrace.Elsa.Abp.Localization;
using BioTrace.Elsa.Abp.Permissions;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace BioTrace.Elsa.Abp.Permissions;

public class AbpElsaPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var elsaGroup = context.AddGroup(AbpElsaPermissions.GroupName, L("Permission:Abp.Elsa"));

        elsaGroup.AddPermission(
            AbpElsaPermissions.Admin,
            L("Permission:Abp.Elsa.Admin"),
            multiTenancySide: MultiTenancySides.Host);

        elsaGroup.AddPermission(
            AbpElsaPermissions.NotReadOnly,
            L("Permission:Abp.Elsa.NotReadOnly"),
            multiTenancySide: MultiTenancySides.Both);

        var definitions = elsaGroup.AddPermission(
            AbpElsaPermissions.WorkflowDefinitions.Default,
            L("Permission:Abp.Elsa.WorkflowDefinitions"),
            multiTenancySide: MultiTenancySides.Tenant);
        definitions.AddChild(
            AbpElsaPermissions.WorkflowDefinitions.Read,
            L("Permission:Abp.Elsa.WorkflowDefinitions.Read"),
            multiTenancySide: MultiTenancySides.Tenant);
        definitions.AddChild(
            AbpElsaPermissions.WorkflowDefinitions.Write,
            L("Permission:Abp.Elsa.WorkflowDefinitions.Write"),
            multiTenancySide: MultiTenancySides.Tenant);
        definitions.AddChild(
            AbpElsaPermissions.WorkflowDefinitions.Publish,
            L("Permission:Abp.Elsa.WorkflowDefinitions.Publish"),
            multiTenancySide: MultiTenancySides.Tenant);
        definitions.AddChild(
            AbpElsaPermissions.WorkflowDefinitions.Retract,
            L("Permission:Abp.Elsa.WorkflowDefinitions.Retract"),
            multiTenancySide: MultiTenancySides.Tenant);
        definitions.AddChild(
            AbpElsaPermissions.WorkflowDefinitions.Delete,
            L("Permission:Abp.Elsa.WorkflowDefinitions.Delete"),
            multiTenancySide: MultiTenancySides.Tenant);

        var instances = elsaGroup.AddPermission(
            AbpElsaPermissions.WorkflowInstances.Default,
            L("Permission:Abp.Elsa.WorkflowInstances"),
            multiTenancySide: MultiTenancySides.Tenant);
        instances.AddChild(
            AbpElsaPermissions.WorkflowInstances.Read,
            L("Permission:Abp.Elsa.WorkflowInstances.Read"),
            multiTenancySide: MultiTenancySides.Tenant);
        instances.AddChild(
            AbpElsaPermissions.WorkflowInstances.Write,
            L("Permission:Abp.Elsa.WorkflowInstances.Write"),
            multiTenancySide: MultiTenancySides.Tenant);
        instances.AddChild(
            AbpElsaPermissions.WorkflowInstances.Delete,
            L("Permission:Abp.Elsa.WorkflowInstances.Delete"),
            multiTenancySide: MultiTenancySides.Tenant);
        instances.AddChild(
            AbpElsaPermissions.WorkflowInstances.Cancel,
            L("Permission:Abp.Elsa.WorkflowInstances.Cancel"),
            multiTenancySide: MultiTenancySides.Tenant);
        instances.AddChild(
            AbpElsaPermissions.WorkflowInstances.Execute,
            L("Permission:Abp.Elsa.WorkflowInstances.Execute"),
            multiTenancySide: MultiTenancySides.Tenant);

        var alterations = elsaGroup.AddPermission(
            AbpElsaPermissions.Alterations.Default,
            L("Permission:Abp.Elsa.Alterations"),
            multiTenancySide: MultiTenancySides.Tenant);
        alterations.AddChild(
            AbpElsaPermissions.Alterations.Read,
            L("Permission:Abp.Elsa.Alterations.Read"),
            multiTenancySide: MultiTenancySides.Tenant);
        alterations.AddChild(
            AbpElsaPermissions.Alterations.Write,
            L("Permission:Abp.Elsa.Alterations.Write"),
            multiTenancySide: MultiTenancySides.Tenant);

        var tasks = elsaGroup.AddPermission(
            AbpElsaPermissions.Tasks.Default,
            L("Permission:Abp.Elsa.Tasks"),
            multiTenancySide: MultiTenancySides.Tenant);
        tasks.AddChild(
            AbpElsaPermissions.Tasks.Complete,
            L("Permission:Abp.Elsa.Tasks.Complete"),
            multiTenancySide: MultiTenancySides.Tenant);

        var http = elsaGroup.AddPermission(
            AbpElsaPermissions.HttpEndpoints.Default,
            L("Permission:Abp.Elsa.HttpEndpoints"),
            multiTenancySide: MultiTenancySides.Tenant);
        http.AddChild(
            AbpElsaPermissions.HttpEndpoints.Invoke,
            L("Permission:Abp.Elsa.HttpEndpoints.Invoke"),
            multiTenancySide: MultiTenancySides.Tenant);
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<AbpResource>(name);
    }
}
