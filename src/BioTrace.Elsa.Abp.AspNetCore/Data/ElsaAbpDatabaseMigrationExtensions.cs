using Microsoft.Extensions.DependencyInjection;

namespace BioTrace.Elsa.Abp.Data;

public static class ElsaAbpDatabaseMigrationExtensions
{
    /// <summary>
    /// Applies Elsa Management/Runtime EF migrations. Use from DbMigrator or CI pipelines when
    /// <see cref="ElsaAbpOptions.RunMigrations"/> is disabled at runtime.
    /// </summary>
    public static Task MigrateElsaDatabasesAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        return serviceProvider.MigrateElsaDatabasesAsync(force: false, cancellationToken);
    }

    public static async Task MigrateElsaDatabasesAsync(
        this IServiceProvider serviceProvider,
        bool force,
        CancellationToken cancellationToken = default)
    {
        var migrator = serviceProvider.GetRequiredService<IElsaDatabaseMigrator>();
        await migrator.MigrateAsync(force, cancellationToken);
    }
}
