namespace BioTrace.Elsa.Abp.Studio.Services;

public interface IElsaAbpStudioPermissionService
{
    Task<IReadOnlyList<string>> GetPermissionsAsync(CancellationToken cancellationToken = default);

    Task<bool> CanWriteWorkflowDefinitionsAsync(CancellationToken cancellationToken = default);

    void InvalidateCache();
}
