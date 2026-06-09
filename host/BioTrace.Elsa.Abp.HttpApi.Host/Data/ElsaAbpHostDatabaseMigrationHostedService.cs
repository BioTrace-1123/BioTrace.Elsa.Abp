using BioTrace.Elsa.Abp.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.TenantManagement.EntityFrameworkCore;
using Volo.Abp.Uow;

namespace BioTrace.Elsa.Abp.Data;

public class ElsaAbpHostDatabaseMigrationHostedService : IHostedService, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostEnvironment _hostEnvironment;

    public ElsaAbpHostDatabaseMigrationHostedService(
        IServiceProvider serviceProvider,
        IHostEnvironment hostEnvironment)
    {
        _serviceProvider = serviceProvider;
        _hostEnvironment = hostEnvironment;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_hostEnvironment.IsDevelopment())
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var unitOfWorkManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

        using (var uow = unitOfWorkManager.Begin(requiresNew: true))
        {
            await MigrateAsync<AbpDbContext>(scope.ServiceProvider, cancellationToken);
            await MigrateAsync<IdentityDbContext>(scope.ServiceProvider, cancellationToken);
            await MigrateAsync<PermissionManagementDbContext>(scope.ServiceProvider, cancellationToken);
            await MigrateAsync<OpenIddictDbContext>(scope.ServiceProvider, cancellationToken);
            await MigrateAsync<TenantManagementDbContext>(scope.ServiceProvider, cancellationToken);
            await uow.CompleteAsync();
        }

        var dataSeeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
        await dataSeeder.SeedAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual async Task MigrateAsync<TDbContext>(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        where TDbContext : DbContext, IEfCoreDbContext
    {
        var dbContext = await serviceProvider.GetRequiredService<IDbContextProvider<TDbContext>>()
            .GetDbContextAsync();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
