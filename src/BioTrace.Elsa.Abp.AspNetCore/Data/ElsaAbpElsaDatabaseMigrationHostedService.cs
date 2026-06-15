using Microsoft.Extensions.Hosting;
using Volo.Abp.DependencyInjection;

namespace BioTrace.Elsa.Abp.Data;

/// <summary>
/// Applies Elsa Management/Runtime EF migrations at application startup when <see cref="ElsaAbpOptions.RunMigrations"/> is enabled.
/// </summary>
public class ElsaAbpElsaDatabaseMigrationHostedService : IHostedService, ITransientDependency
{
    private readonly ElsaAbpElsaDatabaseMigrator _migrator;

    public ElsaAbpElsaDatabaseMigrationHostedService(ElsaAbpElsaDatabaseMigrator migrator)
    {
        _migrator = migrator;
    }

    public virtual Task StartAsync(CancellationToken cancellationToken)
    {
        return _migrator.MigrateAsync(cancellationToken);
    }

    public virtual Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
