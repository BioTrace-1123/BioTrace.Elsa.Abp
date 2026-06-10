using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Volo.Abp.DependencyInjection;

namespace BioTrace.Elsa.Abp.Data;

/// <summary>
/// Applies Elsa Management/Runtime EF migrations before workflow demo seeders run in Development.
/// </summary>
public class ElsaAbpElsaDatabaseMigrationHostedService : IHostedService, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostEnvironment _hostEnvironment;

    public ElsaAbpElsaDatabaseMigrationHostedService(
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
        await MigrateAsync<ManagementElsaDbContext>(scope.ServiceProvider, cancellationToken);
        await MigrateAsync<RuntimeElsaDbContext>(scope.ServiceProvider, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual async Task MigrateAsync<TDbContext>(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        where TDbContext : DbContext
    {
        await using var dbContext = serviceProvider.GetRequiredService<TDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
