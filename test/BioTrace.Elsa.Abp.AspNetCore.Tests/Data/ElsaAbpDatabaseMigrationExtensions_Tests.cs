using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Shouldly;
using Xunit;

namespace BioTrace.Elsa.Abp.Data;

public class ElsaAbpDatabaseMigrationExtensions_Tests
{
    [Fact]
    public async Task MigrateElsaDatabasesAsync_should_invoke_force_migration()
    {
        var migrator = Substitute.For<IElsaDatabaseMigrator>();

        var services = new ServiceCollection();
        services.AddSingleton(migrator);
        var serviceProvider = services.BuildServiceProvider();

        await serviceProvider.MigrateElsaDatabasesAsync(force: true);

        await migrator.Received(1).MigrateAsync(true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ElsaAbpAspNetCoreModule_should_register_migrator_when_run_migrations_disabled()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IHostEnvironment>());
        services.AddLogging();
        services.Configure<ElsaAbpOptions>(options => options.RunMigrations = false);

        services.AddSingleton<ElsaAbpElsaDatabaseMigrator>();
        services.AddSingleton<IElsaDatabaseMigrator>(sp => sp.GetRequiredService<ElsaAbpElsaDatabaseMigrator>());

        var serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetService<IElsaDatabaseMigrator>().ShouldNotBeNull();
        serviceProvider.GetService<ElsaAbpElsaDatabaseMigrator>().ShouldNotBeNull();
    }
}
