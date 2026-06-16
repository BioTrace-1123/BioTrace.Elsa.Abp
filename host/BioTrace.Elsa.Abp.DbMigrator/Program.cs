using BioTrace.Elsa.Abp;
using BioTrace.Elsa.Abp.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Volo.Abp;
using Volo.Abp.Autofac;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .AddEnvironmentVariables()
        .AddCommandLine(args)
        .Build();

    using var application = await AbpApplicationFactory.CreateAsync<ElsaAbpDbMigratorModule>(options =>
    {
        options.UseAutofac();
        options.Services.ReplaceConfiguration(configuration);
        options.Services.AddSingleton<IHostEnvironment>(new MigratorHostEnvironment());
    });

    await application.InitializeAsync();

    var serviceProvider = application.ServiceProvider;
    await serviceProvider.GetRequiredService<ElsaAbpAbpDatabaseMigrator>().MigrateAsync();
    await serviceProvider.MigrateElsaDatabasesAsync(force: true);

    await application.ShutdownAsync();

    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "DbMigrator terminated unexpectedly.");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

file sealed class MigratorHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Production;
    public string ApplicationName { get; set; } = "BioTrace.Elsa.Abp.DbMigrator";
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    public IFileProvider ContentRootFileProvider { get; set; } = null!;
}
