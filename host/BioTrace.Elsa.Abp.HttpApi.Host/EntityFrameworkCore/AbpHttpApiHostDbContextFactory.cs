using BioTrace.Elsa.Abp.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BioTrace.Elsa.Abp.EntityFrameworkCore;

public class AbpHttpApiHostDbContextFactory : IDesignTimeDbContextFactory<AbpDbContext>
{
    public AbpDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString(AbpDbProperties.ConnectionStringName)!;
        var builder = new DbContextOptionsBuilder<AbpDbContext>();
        builder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsAssembly(typeof(AbpHttpApiHostDbContextFactory).Assembly.FullName));
        return new AbpDbContext(builder.Options);
    }
}
