using BioTrace.Elsa.Abp.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BioTrace.Elsa.Abp.Fixtures;

public class ElsaAbpWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string BaseUrl = "http://127.0.0.1:17443";

    private static bool _postgresChecked;
    private static bool _postgresAvailable;

    public static async Task EnsurePostgresReadyAsync(CancellationToken cancellationToken = default)
    {
        if (_postgresChecked)
        {
            if (!_postgresAvailable)
            {
                throw new InvalidOperationException(PostgresAvailability.SkipReason);
            }

            return;
        }

        _postgresAvailable = await PostgresAvailability.IsAvailableAsync(cancellationToken);
        _postgresChecked = true;

        if (!_postgresAvailable)
        {
            throw new InvalidOperationException(PostgresAvailability.SkipReason);
        }

        await PostgresAvailability.EnsureTestDatabasesAsync(cancellationToken);

        var postgresBase = IntegrationTestPostgresSettings.GetConnectionBase();
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", $"{postgresBase};Database=BioTrace_Abp_Test");
        Environment.SetEnvironmentVariable("ConnectionStrings__Elsa", $"{postgresBase};Database=BioTrace_Elsa_Test");
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
            var defaultConnection = $"{postgresBase};Database=BioTrace_Abp_Test";
            var elsaConnection = $"{postgresBase};Database=BioTrace_Elsa_Test";

            // Dev Container sets ConnectionStrings__* env vars; override so Elsa uses the test database.
            Environment.SetEnvironmentVariable("ConnectionStrings__Default", defaultConnection);
            Environment.SetEnvironmentVariable("ConnectionStrings__Elsa", elsaConnection);

            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:SelfUrl"] = BaseUrl,
                ["App:CorsOrigins:0"] = BaseUrl,
                ["AuthServer:Authority"] = BaseUrl,
                ["AuthServer:AllowPasswordGrantForIntegrationTests"] = "true",
                ["AuthServer:SwaggerClientId"] = "BioTrace_Elsa_Abp_Swagger",
                ["ConnectionStrings:Default"] = defaultConnection,
                ["ConnectionStrings:Elsa"] = elsaConnection,
                ["Elsa:RunMigrations"] = "true",
                ["Elsa:EnableWorkflowsApi"] = "true",
                ["Elsa:EnableElsaSwagger"] = "false",
                ["Elsa:EnableHttpActivities"] = "false",
                ["Elsa:EnablePermissionClaimsBridge"] = "true",
                ["Elsa:EnableMultiTenancy"] = "true",
                ["Elsa:DisableElsaEndpointSecurity"] = "false",
                ["OpenIddict:Applications:IntegrationTests:ClientId"] = OpenIddictTokenClient.ClientId,
                ["OpenIddict:Applications:IntegrationTests:ClientSecret"] = OpenIddictTokenClient.ClientSecret,
                ["Host:WaitForDatabase"] = "false"
            });
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        client.BaseAddress = new Uri(BaseUrl);
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Forwarded-Proto", "https");
    }
}
