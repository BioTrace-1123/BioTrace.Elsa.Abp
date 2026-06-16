using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;
using Xunit;

namespace BioTrace.Elsa.Abp.Data;

public class ElsaAbpElsaDatabaseMigrator_Tests
{
    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(false, false, true, true)]
    [InlineData(false, true, false, false)]
    [InlineData(false, true, true, true)]
    [InlineData(true, false, false, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, false)]
    [InlineData(true, true, true, true)]
    public void ShouldMigrate_should_respect_options_and_force_flag(
        bool runMigrations,
        bool migrateOnlyInDevelopment,
        bool isDevelopment,
        bool force)
    {
        var migrator = CreateMigrator(
            new ElsaAbpOptions
            {
                RunMigrations = runMigrations,
                MigrateOnlyInDevelopment = migrateOnlyInDevelopment
            },
            isDevelopment);

        migrator.ShouldMigrate(force: force).ShouldBe(force || (runMigrations && (isDevelopment || !migrateOnlyInDevelopment)));
    }

    [Fact]
    public void ShouldMigrate_should_be_false_when_run_migrations_disabled_without_force()
    {
        var migrator = CreateMigrator(
            new ElsaAbpOptions { RunMigrations = false },
            isDevelopment: false);

        migrator.ShouldMigrate().ShouldBeFalse();
        migrator.ShouldMigrate(force: true).ShouldBeTrue();
    }

    private static ElsaAbpElsaDatabaseMigrator CreateMigrator(ElsaAbpOptions options, bool isDevelopment)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(
            isDevelopment ? Environments.Development : Environments.Production);

        return new ElsaAbpElsaDatabaseMigrator(
            new ServiceCollection().BuildServiceProvider(),
            environment,
            Options.Create(options));
    }
}
