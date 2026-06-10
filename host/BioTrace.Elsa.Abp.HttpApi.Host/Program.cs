using BioTrace.Elsa.Abp;
using BioTrace.Elsa.Abp.Data;
using Serilog;
using Serilog.Events;
using Volo.Abp.Data;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseAutofac();
    builder.Host.UseSerilog();

    await builder.AddApplicationAsync<AbpHttpApiHostModule>();

    var app = builder.Build();
    await app.InitializeApplicationAsync();

    if (!IsEfDesignTime() && app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ElsaAbpHostDatabaseMigrationHostedService>()
            .StartAsync(CancellationToken.None);
        await scope.ServiceProvider.GetRequiredService<ElsaAbpElsaDatabaseMigrationHostedService>()
            .StartAsync(CancellationToken.None);
        await scope.ServiceProvider.GetRequiredService<IDataSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<ElsaAbpTenantDemoWorkflowSeeder>().SeedAsync();
    }

    await app.RunAsync();

    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly.");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

static bool IsEfDesignTime()
{
    return string.Equals(
        Environment.GetEnvironmentVariable("DOTNET_HOST_FACTORY_RESOLVER"),
        "EF",
        StringComparison.OrdinalIgnoreCase);
}

public partial class Program;
