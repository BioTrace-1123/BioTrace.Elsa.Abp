using Volo.Abp.DependencyInjection;

namespace BioTrace.Elsa.Abp.Data;

/// <summary>
/// Registers <see cref="ElsaAbpOpenIddictDataSeedContributor"/> in the demonstration host.
/// </summary>
public class ElsaAbpHostOpenIddictDataSeedContributor : ElsaAbpOpenIddictDataSeedContributor, ITransientDependency
{
    public ElsaAbpHostOpenIddictDataSeedContributor(
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        OpenIddict.Abstractions.IOpenIddictApplicationManager applicationManager,
        OpenIddict.Abstractions.IOpenIddictScopeManager scopeManager,
        Volo.Abp.Uow.IUnitOfWorkManager unitOfWorkManager)
        : base(configuration, applicationManager, scopeManager, unitOfWorkManager)
    {
    }
}
