using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;

namespace BioTrace.Elsa.Abp.EntityFrameworkCore;

public class PermissionManagementDbContextFactory : IDesignTimeDbContextFactory<PermissionManagementDbContext>
{
    public PermissionManagementDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        var connectionString = configuration.GetConnectionString("AbpPermissionManagement")
            ?? configuration.GetConnectionString(AbpDbProperties.ConnectionStringName)!;

        var builder = new DbContextOptionsBuilder<PermissionManagementDbContext>();
        builder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsAssembly(typeof(PermissionManagementDbContextFactory).Assembly.FullName));
        return new PermissionManagementDbContext(builder.Options);
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }
}
