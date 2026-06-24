using System.Net;
using BioTrace.Elsa.Abp.Studio.Services;
using Blazored.FluentValidation;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.Validators;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using Refit;

namespace BioTrace.Elsa.Abp.Studio.Components;

public partial class AbpCreateWorkflowDialog
{
    private readonly WorkflowMetadataModel _metadataModel = new();
    private EditContext _editContext = null!;
    private WorkflowPropertiesModelValidator _validator = null!;
    private FluentValidationValidator _fluentValidationValidator = null!;
    private bool _canCreate = true;

    [Parameter] public string WorkflowName { get; set; } = "New workflow";

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = null!;

    [Inject] private IElsaAbpStudioPermissionService PermissionService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _metadataModel.Name = WorkflowName;
        _editContext = new(_metadataModel);
        _validator = new(WorkflowDefinitionService, Localizer);
        _canCreate = await PermissionService.CanWriteWorkflowDefinitionsAsync();
    }

    private Task OnCancelClicked()
    {
        MudDialog.Cancel();
        return Task.CompletedTask;
    }

    private async Task OnSubmitClicked()
    {
        if (!_canCreate)
        {
            return;
        }

        if (!await _fluentValidationValidator.ValidateAsync())
        {
            return;
        }

        await OnValidSubmit();
    }

    private async Task OnValidSubmit()
    {
        try
        {
            var result = await WorkflowDefinitionService.CreateNewDefinitionAsync(
                _metadataModel.Name!,
                _metadataModel.Description!);

            MudDialog.Close(result);
        }
        catch (ApiException ex) when (ex.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
        {
            var message = ex.StatusCode == HttpStatusCode.Forbidden
                ? "You do not have permission to create workflow definitions."
                : "Authentication is required to create workflow definitions.";

            MudDialog.Close(new Result<WorkflowDefinition, ValidationErrors>(
                new ValidationErrors([new ValidationError(message)])));
        }
    }
}
