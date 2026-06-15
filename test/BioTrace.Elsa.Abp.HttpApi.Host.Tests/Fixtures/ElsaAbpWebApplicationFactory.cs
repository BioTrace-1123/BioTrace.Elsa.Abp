using BioTrace.Elsa.Abp.Helpers;
using BioTrace.Elsa.Abp.IntegrationTesting;

namespace BioTrace.Elsa.Abp.Fixtures;

public class ElsaAbpWebApplicationFactory : ElsaAbpIntegrationTestWebApplicationFactory<Program>
{
    public const string DemoBaseUrl = "http://127.0.0.1:17443";

    private static bool _postgresChecked;
    private static bool _postgresAvailable;

    protected override string BaseUrl => DemoBaseUrl;

    protected override string AbpDatabaseName => "BioTrace_Abp_Test";

    protected override string ElsaDatabaseName => "BioTrace_Elsa_Test";

    public new static async Task EnsurePostgresReadyAsync(CancellationToken cancellationToken = default)
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

        await PostgresAvailability.EnsureTestDatabasesAsync("BioTrace_Abp_Test", "BioTrace_Elsa_Test", cancellationToken);

        var postgresBase = IntegrationTestPostgresSettings.GetConnectionBase();
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", $"{postgresBase};Database=BioTrace_Abp_Test");
        Environment.SetEnvironmentVariable("ConnectionStrings__Elsa", $"{postgresBase};Database=BioTrace_Elsa_Test");
    }

    protected override void ConfigureTestSettings(IDictionary<string, string?> settings)
    {
        settings["AuthServer:AllowPasswordGrantForIntegrationTests"] = "true";
        settings["AuthServer:SwaggerClientId"] = "BioTrace_Elsa_Abp_Swagger";
        settings["Elsa:EnableHttpActivities"] = "false";
        settings["Elsa:EnableMultiTenancy"] = "true";
        settings["Elsa:DisableElsaEndpointSecurity"] = "false";
        settings["OpenIddict:Applications:IntegrationTests:ClientId"] = ElsaAbpIntegrationTestTokenClient.ClientId;
        settings["OpenIddict:Applications:IntegrationTests:ClientSecret"] = ElsaAbpIntegrationTestTokenClient.ClientSecret;
        settings["Host:WaitForDatabase"] = "false";
    }
}
