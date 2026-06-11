using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Volo.Abp.OpenIddict.EntityFrameworkCore;

namespace BioTrace.Elsa.Abp.EntityFrameworkCore;

public class OpenIddictDbContextFactory : IDesignTimeDbContextFactory<OpenIddictDbContext>
{
    public OpenIddictDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        var connectionString = configuration.GetConnectionString("AbpOpenIddict")
            ?? configuration.GetConnectionString(AbpDbProperties.ConnectionStringName)!;

        var builder = new DbContextOptionsBuilder<OpenIddictDbContext>();
        builder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsAssembly(typeof(OpenIddictDbContextFactory).Assembly.FullName));
        return new OpenIddictDbContext(builder.Options);
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
