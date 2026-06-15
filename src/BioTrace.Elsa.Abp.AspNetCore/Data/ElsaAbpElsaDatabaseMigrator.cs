using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BioTrace.Elsa.Abp.Data;

/// <summary>
/// Applies Elsa Management/Runtime EF Core migrations for the configured Elsa persistence provider.
/// </summary>
public class ElsaAbpElsaDatabaseMigrator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ElsaAbpOptions _options;

    public ElsaAbpElsaDatabaseMigrator(
        IServiceProvider serviceProvider,
        IHostEnvironment hostEnvironment,
        IOptions<ElsaAbpOptions> options)
    {
        _serviceProvider = serviceProvider;
        _hostEnvironment = hostEnvironment;
        _options = options.Value;
    }

    public virtual bool ShouldMigrate()
    {
        if (!_options.RunMigrations)
        {
            return false;
        }

        if (_options.MigrateOnlyInDevelopment && !_hostEnvironment.IsDevelopment())
        {
            return false;
        }

        return true;
    }

    public virtual async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        if (!ShouldMigrate())
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        await MigrateDbContextAsync<ManagementElsaDbContext>(scope.ServiceProvider, cancellationToken);
        await MigrateDbContextAsync<RuntimeElsaDbContext>(scope.ServiceProvider, cancellationToken);
    }

    protected virtual async Task MigrateDbContextAsync<TDbContext>(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        where TDbContext : DbContext
    {
        await using var dbContext = serviceProvider.GetRequiredService<TDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
