using BioTrace.Elsa.Abp.MultiTenancy;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;

namespace BioTrace.Elsa.Abp.Data;

/// <summary>
/// Seeds demo workflow definitions per tenant after Elsa is fully initialized.
/// </summary>
public class ElsaAbpTenantDemoWorkflowSeeder : ITransientDependency
{
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ICurrentTenant _currentTenant;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantNormalizer _tenantNormalizer;
    private readonly IElsaAbpTenantMapper _tenantMapper;
    private readonly ITenantsProvider _tenantsProvider;
    private readonly ITenantAccessor _tenantAccessor;
    private readonly IWorkflowDefinitionPublisher _workflowDefinitionPublisher;
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly ILogger<ElsaAbpTenantDemoWorkflowSeeder> _logger;

    public ElsaAbpTenantDemoWorkflowSeeder(
        IHostEnvironment hostEnvironment,
        ICurrentTenant currentTenant,
        ITenantRepository tenantRepository,
        ITenantNormalizer tenantNormalizer,
        IElsaAbpTenantMapper tenantMapper,
        ITenantsProvider tenantsProvider,
        ITenantAccessor tenantAccessor,
        IWorkflowDefinitionPublisher workflowDefinitionPublisher,
        IWorkflowDefinitionStore workflowDefinitionStore,
        ILogger<ElsaAbpTenantDemoWorkflowSeeder> logger)
    {
        _hostEnvironment = hostEnvironment;
        _currentTenant = currentTenant;
        _tenantRepository = tenantRepository;
        _tenantNormalizer = tenantNormalizer;
        _tenantMapper = tenantMapper;
        _tenantsProvider = tenantsProvider;
        _tenantAccessor = tenantAccessor;
        _workflowDefinitionPublisher = workflowDefinitionPublisher;
        _workflowDefinitionStore = workflowDefinitionStore;
        _logger = logger;
    }

    public virtual async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_hostEnvironment.IsDevelopment())
        {
            return;
        }

        await SeedTenantWorkflowAsync(
            ElsaAbpMultiTenancySeedData.TenantAName,
            "DemoTenantAWorkflow",
            "Tenant A Demo Workflow",
            cancellationToken);

        await SeedTenantWorkflowAsync(
            ElsaAbpMultiTenancySeedData.TenantBName,
            "DemoTenantBWorkflow",
            "Tenant B Demo Workflow",
            cancellationToken);
    }

    protected virtual async Task SeedTenantWorkflowAsync(
        string tenantName,
        string definitionId,
        string workflowName,
        CancellationToken cancellationToken)
    {
        var normalizedName = _tenantNormalizer.NormalizeName(tenantName);
        var tenant = await _tenantRepository.FindByNameAsync(normalizedName, cancellationToken: cancellationToken);
        if (tenant == null)
        {
            _logger.LogWarning("Skip demo workflow seed: ABP tenant '{TenantName}' was not found.", tenantName);
            return;
        }

        var elsaTenantId = _tenantMapper.ToElsaTenantId(tenant.Id);
        var elsaTenant = await _tenantsProvider.FindAsync(TenantFilter.ById(elsaTenantId), cancellationToken);
        if (elsaTenant == null)
        {
            _logger.LogWarning(
                "Skip demo workflow seed: Elsa tenant '{ElsaTenantId}' was not found for '{TenantName}'.",
                elsaTenantId,
                tenantName);
            return;
        }

        using (_currentTenant.Change(tenant.Id))
        using (_tenantAccessor.PushContext(elsaTenant))
        {
                var existing = await _workflowDefinitionStore.FindAsync(
                    new WorkflowDefinitionFilter
                    {
                        DefinitionId = definitionId
                    },
                    cancellationToken);

                if (existing != null)
                {
                    return;
                }

                var draft = await _workflowDefinitionPublisher.NewAsync(new Sequence(), cancellationToken);
                draft.DefinitionId = definitionId;
                draft.Name = workflowName;
                draft.Description = "Seeded demo workflow for local multi-tenancy verification.";

                await _workflowDefinitionPublisher.SaveDraftAsync(draft, cancellationToken);

                _logger.LogInformation(
                    "Seeded demo workflow '{DefinitionId}' for tenant '{TenantName}' (Elsa TenantId: {ElsaTenantId}).",
                    definitionId,
                    tenantName,
                    elsaTenantId);
        }
    }
}
