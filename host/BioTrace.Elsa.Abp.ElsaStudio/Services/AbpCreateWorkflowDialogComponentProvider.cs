using BioTrace.Elsa.Abp.ElsaStudio.Components;
using Elsa.Studio.Workflows.Contracts;

namespace BioTrace.Elsa.Abp.ElsaStudio.Services;

public class AbpCreateWorkflowDialogComponentProvider : ICreateWorkflowDialogComponentProvider
{
    public virtual Type GetComponentType() => typeof(AbpCreateWorkflowDialog);
}
