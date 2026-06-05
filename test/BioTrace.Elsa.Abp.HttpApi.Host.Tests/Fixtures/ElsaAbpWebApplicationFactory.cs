using BioTrace.Elsa.Abp.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
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
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.UseSetting(WebHostDefaults.ServerUrlsKey, BaseUrl);

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            var postgresBase =
                "Host=localhost;Port=5432;Username=postgres;Password=postgres";

            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:SelfUrl"] = BaseUrl,
                ["App:CorsOrigins:0"] = BaseUrl,
                ["AuthServer:Authority"] = BaseUrl,
                ["AuthServer:AllowPasswordGrantForIntegrationTests"] = "true",
                ["AuthServer:SwaggerClientId"] = "BioTrace_Elsa_Abp_Swagger",
                ["ConnectionStrings:Abp"] = $"{postgresBase};Database=BioTrace_Abp_Test",
                ["ConnectionStrings:Elsa"] = $"{postgresBase};Database=BioTrace_Elsa_Test",
                ["Elsa:RunMigrations"] = "true",
                ["Elsa:EnableWorkflowsApi"] = "true",
                ["Elsa:EnableElsaSwagger"] = "false",
                ["Elsa:EnableHttpActivities"] = "false",
                ["Elsa:EnablePermissionClaimsBridge"] = "true",
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
    }
}
