using Microsoft.Extensions.DependencyInjection;

namespace BioTrace.Elsa.Abp.Data;

public static class ElsaAbpDatabaseMigrationExtensions
{
    /// <summary>
    /// Applies Elsa Management/Runtime EF migrations. Use from DbMigrator or CI pipelines when
    /// <see cref="ElsaAbpOptions.RunMigrations"/> is disabled at runtime.
    /// </summary>
    public static async Task MigrateElsaDatabasesAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var migrator = serviceProvider.GetRequiredService<ElsaAbpElsaDatabaseMigrator>();
        await migrator.MigrateAsync(cancellationToken);
    }
}
