using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Volo.Abp.TenantManagement.EntityFrameworkCore;

namespace BioTrace.Elsa.Abp.EntityFrameworkCore;

public class TenantManagementDbContextFactory : IDesignTimeDbContextFactory<TenantManagementDbContext>
{
    public TenantManagementDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        var connectionString = configuration.GetConnectionString("AbpTenantManagement")
            ?? configuration.GetConnectionString(AbpDbProperties.ConnectionStringName)!;

        var builder = new DbContextOptionsBuilder<TenantManagementDbContext>();
        builder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsAssembly(typeof(TenantManagementDbContextFactory).Assembly.FullName));
        return new TenantManagementDbContext(builder.Options);
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();
    }
}
