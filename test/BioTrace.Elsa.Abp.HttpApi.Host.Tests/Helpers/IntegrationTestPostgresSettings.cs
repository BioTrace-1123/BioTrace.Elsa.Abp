namespace BioTrace.Elsa.Abp.Helpers;

public static class IntegrationTestPostgresSettings
{
    public const string HostEnvironmentVariable = "INTEGRATION_TEST_POSTGRES_HOST";

    public static string Host =>
        Environment.GetEnvironmentVariable(HostEnvironmentVariable) ?? "localhost";

    public static string GetConnectionBase() =>
        $"Host={Host};Port=5432;Username=postgres;Password=postgres";

    public static string GetAdminConnectionString() =>
        $"{GetConnectionBase()};Database=postgres";

    public static string GetSkipReason()
    {
        if (string.Equals(Host, "postgres", StringComparison.OrdinalIgnoreCase))
        {
            return "PostgreSQL is not available. In Dev Container, ensure the compose postgres service is healthy. "
                   + "Test databases BioTrace_Abp_Test / BioTrace_Elsa_Test are created automatically when Postgres is reachable.";
        }

        return "PostgreSQL is not available. On the host machine run `docker compose up -d` first, "
               + "or set INTEGRATION_TEST_POSTGRES_HOST=postgres inside Dev Container. "
               + "Test databases BioTrace_Abp_Test / BioTrace_Elsa_Test are created automatically when Postgres is reachable.";
    }
}
