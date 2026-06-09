using BioTrace.Elsa.Abp.Studio.Components;
using Elsa.Studio.Workflows.Contracts;

namespace BioTrace.Elsa.Abp.Studio.Services;

public class AbpCreateWorkflowDialogComponentProvider : ICreateWorkflowDialogComponentProvider
{
    public virtual Type GetComponentType() => typeof(AbpCreateWorkflowDialog);
}
