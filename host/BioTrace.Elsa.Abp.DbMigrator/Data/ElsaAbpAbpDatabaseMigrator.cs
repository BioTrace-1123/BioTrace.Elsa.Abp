using BioTrace.Elsa.Abp.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.TenantManagement.EntityFrameworkCore;
using Volo.Abp.Uow;

namespace BioTrace.Elsa.Abp.Data;

/// <summary>
/// Migrates ABP business databases (Identity, OpenIddict, Permission, TenantManagement).
/// </summary>
public class ElsaAbpAbpDatabaseMigrator : ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public ElsaAbpAbpDatabaseMigrator(
        IServiceProvider serviceProvider,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _serviceProvider = serviceProvider;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public virtual async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();

        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            await MigrateDbContextAsync<AbpDbContext>(scope.ServiceProvider, cancellationToken);
            await MigrateDbContextAsync<IdentityDbContext>(scope.ServiceProvider, cancellationToken);
            await MigrateDbContextAsync<PermissionManagementDbContext>(scope.ServiceProvider, cancellationToken);
            await MigrateDbContextAsync<OpenIddictDbContext>(scope.ServiceProvider, cancellationToken);
            await MigrateDbContextAsync<TenantManagementDbContext>(scope.ServiceProvider, cancellationToken);
            await uow.CompleteAsync(cancellationToken);
        }
    }

    protected virtual async Task MigrateDbContextAsync<TDbContext>(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        where TDbContext : DbContext, IEfCoreDbContext
    {
        var dbContext = await serviceProvider.GetRequiredService<IDbContextProvider<TDbContext>>()
            .GetDbContextAsync();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
