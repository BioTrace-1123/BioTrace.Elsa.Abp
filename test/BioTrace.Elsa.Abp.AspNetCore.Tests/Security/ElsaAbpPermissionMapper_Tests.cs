using BioTrace.Elsa.Abp.Permissions;
using BioTrace.Elsa.Abp.Security;
using Shouldly;
using Xunit;

namespace BioTrace.Elsa.Abp.Security;

public class ElsaAbpPermissionMapper_Tests
{
    private readonly ElsaAbpPermissionMapper _mapper = new();

    [Fact]
    public void Admin_should_map_to_wildcard()
    {
        var result = _mapper.MapToElsaPermissions([AbpElsaPermissions.Admin]);

        result.ShouldBe([ElsaApiPermissionNames.Wildcard]);
    }

    [Fact]
    public void WorkflowDefinitions_Read_should_map_to_read_claim()
    {
        var result = _mapper.MapToElsaPermissions([AbpElsaPermissions.WorkflowDefinitions.Read]);

        result.ShouldContain(ElsaApiPermissionNames.WorkflowDefinitions.Read);
        result.ShouldContain(ElsaApiPermissionNames.StudioDescriptors.ReadCommitStrategies);
        result.ShouldContain(ElsaApiPermissionNames.StudioDescriptors.ReadActivityDescriptors);
    }

    [Fact]
    public void WorkflowDefinitions_Default_should_map_all_definition_claims()
    {
        var result = _mapper.MapToElsaPermissions([AbpElsaPermissions.WorkflowDefinitions.Default]);

        result.ShouldContain(ElsaApiPermissionNames.WorkflowDefinitions.Read);
        result.ShouldContain(ElsaApiPermissionNames.WorkflowDefinitions.Write);
        result.ShouldContain(ElsaApiPermissionNames.WorkflowDefinitions.Publish);
        result.ShouldContain(ElsaApiPermissionNames.WorkflowDefinitions.Delete);
        result.ShouldContain(ElsaApiPermissionNames.WorkflowDefinitionActions.Execute);
    }

    [Fact]
    public void WorkflowDefinitions_Write_should_map_studio_action_claims()
    {
        var result = _mapper.MapToElsaPermissions([AbpElsaPermissions.WorkflowDefinitions.Write]);

        result.ShouldContain(ElsaApiPermissionNames.WorkflowDefinitions.Write);
        result.ShouldContain(ElsaApiPermissionNames.WorkflowDefinitionActions.Refresh);
        result.ShouldContain(ElsaApiPermissionNames.WorkflowDefinitionActions.Reload);
    }
}
