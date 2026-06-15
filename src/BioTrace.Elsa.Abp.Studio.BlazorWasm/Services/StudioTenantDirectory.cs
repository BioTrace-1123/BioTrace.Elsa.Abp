using BioTrace.Elsa.Abp.Studio.Models;
using Microsoft.Extensions.Options;

namespace BioTrace.Elsa.Abp.Studio.Services;

/// <summary>
/// Resolves tenant options from ABP TenantManagement API when enabled, otherwise from static configuration.
/// </summary>
public class StudioTenantDirectory : IStudioTenantDirectory
{
    private readonly BioTraceElsaAbpStudioOptions _options;
    private readonly AbpApiStudioTenantDirectory _abpApiDirectory;
    private readonly ConfigurationStudioTenantDirectory _configurationDirectory;

    public StudioTenantDirectory(
        IOptions<BioTraceElsaAbpStudioOptions> options,
        AbpApiStudioTenantDirectory abpApiDirectory,
        ConfigurationStudioTenantDirectory configurationDirectory)
    {
        _options = options.Value;
        _abpApiDirectory = abpApiDirectory;
        _configurationDirectory = configurationDirectory;
    }

    public virtual async Task<IReadOnlyList<StudioTenantOption>> GetTenantsAsync(CancellationToken cancellationToken = default)
    {
        var configuredTenants = await _configurationDirectory.GetTenantsAsync(cancellationToken);

        if (_options.Tenancy.UseAbpTenantApi)
        {
            var fromApi = await _abpApiDirectory.GetTenantsAsync(cancellationToken);
            if (fromApi.Count > 0)
            {
                return ApplyConfiguredDisplayNames(fromApi, configuredTenants);
            }
        }

        return configuredTenants;
    }

    protected virtual IReadOnlyList<StudioTenantOption> ApplyConfiguredDisplayNames(
        IReadOnlyList<StudioTenantOption> fromApi,
        IReadOnlyList<StudioTenantOption> configuredTenants)
    {
        return fromApi
            .Select(tenant =>
            {
                var configured = configuredTenants.FirstOrDefault(item =>
                    string.Equals(item.Name, tenant.Name, StringComparison.OrdinalIgnoreCase));

                return new StudioTenantOption
                {
                    Id = tenant.Id,
                    Name = tenant.Name,
                    DisplayName = string.IsNullOrWhiteSpace(configured?.DisplayName)
                        ? tenant.DisplayName
                        : configured.DisplayName
                };
            })
            .ToList();
    }
}
