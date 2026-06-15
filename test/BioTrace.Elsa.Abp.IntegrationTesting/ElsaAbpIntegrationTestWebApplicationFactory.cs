using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BioTrace.Elsa.Abp.IntegrationTesting;

/// <summary>
/// Base <see cref="WebApplicationFactory{TEntryPoint}"/> for BioTrace.Elsa.Abp integration tests against a consumer Host.
/// </summary>
public abstract class ElsaAbpIntegrationTestWebApplicationFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint>
    where TEntryPoint : class
{
    protected virtual string BaseUrl => "http://127.0.0.1:17443";

    protected abstract string AbpDatabaseName { get; }

    protected abstract string ElsaDatabaseName { get; }

    protected virtual void ConfigureTestSettings(IDictionary<string, string?> settings)
    {
    }

    public virtual async Task EnsurePostgresReadyAsync(CancellationToken cancellationToken = default)
    {
        if (!await PostgresAvailability.IsAvailableAsync(cancellationToken))
        {
            throw new InvalidOperationException(PostgresAvailability.SkipReason);
        }

        await PostgresAvailability.EnsureTestDatabasesAsync(AbpDatabaseName, ElsaDatabaseName, cancellationToken);

        var postgresBase = IntegrationTestPostgresSettings.GetConnectionBase();
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", $"{postgresBase};Database={AbpDatabaseName}");
        Environment.SetEnvironmentVariable("ConnectionStrings__Elsa", $"{postgresBase};Database={ElsaDatabaseName}");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.UseSetting(WebHostDefaults.ServerUrlsKey, BaseUrl);

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IStartupFilter, IntegrationTestHttpsStartupFilter>();
        });

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            var postgresBase = IntegrationTestPostgresSettings.GetConnectionBase();
            var defaultConnection = $"{postgresBase};Database={AbpDatabaseName}";
            var elsaConnection = $"{postgresBase};Database={ElsaDatabaseName}";

            Environment.SetEnvironmentVariable("ConnectionStrings__Default", defaultConnection);
            Environment.SetEnvironmentVariable("ConnectionStrings__Elsa", elsaConnection);

            var settings = new Dictionary<string, string?>
            {
                ["App:SelfUrl"] = BaseUrl,
                ["App:CorsOrigins:0"] = BaseUrl,
                ["AuthServer:Authority"] = BaseUrl,
                ["ConnectionStrings:Default"] = defaultConnection,
                ["ConnectionStrings:Elsa"] = elsaConnection,
                ["Elsa:RunMigrations"] = "true",
                ["Elsa:EnableWorkflowsApi"] = "true",
                ["Elsa:EnableElsaSwagger"] = "false",
                ["Elsa:EnablePermissionClaimsBridge"] = "true"
            };

            ConfigureTestSettings(settings);
            configurationBuilder.AddInMemoryCollection(settings);
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        client.BaseAddress = new Uri(BaseUrl);
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Forwarded-Proto", "https");
    }
}
