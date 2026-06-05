namespace BioTrace.Elsa.Abp.ElsaStudio.Services;

public interface IElsaAbpStudioPermissionService
{
    Task<IReadOnlyList<string>> GetPermissionsAsync(CancellationToken cancellationToken = default);

    Task<bool> CanWriteWorkflowDefinitionsAsync(CancellationToken cancellationToken = default);
}
