using Npgsql;

namespace BioTrace.Elsa.Abp.Helpers;

public static class PostgresAvailability
{
    public const string SkipReason = "PostgreSQL is not available. Run `docker compose up -d` and ensure BioTrace_Abp_Test / BioTrace_Elsa_Test exist.";

    private const string AdminConnectionString =
        "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";

    public static async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(AdminConnectionString);
            await connection.OpenAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static async Task EnsureTestDatabasesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(AdminConnectionString);
        await connection.OpenAsync(cancellationToken);

        await EnsureDatabaseAsync(connection, "BioTrace_Abp_Test", cancellationToken);
        await EnsureDatabaseAsync(connection, "BioTrace_Elsa_Test", cancellationToken);
    }

    private static async Task EnsureDatabaseAsync(
        NpgsqlConnection connection,
        string databaseName,
        CancellationToken cancellationToken)
    {
        await using var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = "SELECT 1 FROM pg_database WHERE datname = @name";
        checkCommand.Parameters.AddWithValue("name", databaseName);

        var exists = await checkCommand.ExecuteScalarAsync(cancellationToken) != null;
        if (exists)
        {
            return;
        }

        await using var createCommand = connection.CreateCommand();
        createCommand.CommandText = $"""CREATE DATABASE "{databaseName}" """;
        await createCommand.ExecuteNonQueryAsync(cancellationToken);
    }
}
