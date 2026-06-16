namespace BioTrace.Elsa.Abp.Data;

/// <summary>
/// Applies Elsa Management/Runtime EF Core migrations. Use from DbMigrator or CI with <paramref name="force"/> when
/// <see cref="ElsaAbpOptions.RunMigrations"/> is disabled at runtime.
/// </summary>
public interface IElsaDatabaseMigrator
{
    /// <summary>
    /// Returns whether migration would run for the current options and environment.
    /// </summary>
    bool ShouldMigrate(bool force = false);

    /// <summary>
    /// Migrates Elsa databases when <see cref="ShouldMigrate"/> allows, or when <paramref name="force"/> is true.
    /// </summary>
    Task MigrateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Migrates Elsa databases when <see cref="ShouldMigrate"/> allows, or when <paramref name="force"/> is true.
    /// </summary>
    Task MigrateAsync(bool force, CancellationToken cancellationToken = default);
}
