using Npgsql;

namespace BioTrace.Elsa.Abp.Helpers;

public static class PostgresAvailability
{
    public static string SkipReason => IntegrationTestPostgresSettings.GetSkipReason();

    public static async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(IntegrationTestPostgresSettings.GetAdminConnectionString());
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
        await using var connection = new NpgsqlConnection(IntegrationTestPostgresSettings.GetAdminConnectionString());
        await connection.OpenAsync(cancellationToken);

        await RecreateDatabaseAsync(connection, "BioTrace_Abp_Test", cancellationToken);
        await RecreateDatabaseAsync(connection, "BioTrace_Elsa_Test", cancellationToken);
    }

    private static async Task RecreateDatabaseAsync(
        NpgsqlConnection connection,
        string databaseName,
        CancellationToken cancellationToken)
    {
        await using var terminateCommand = connection.CreateCommand();
        terminateCommand.CommandText = """
            SELECT pg_terminate_backend(pid)
            FROM pg_stat_activity
            WHERE datname = @name AND pid <> pg_backend_pid()
            """;
        terminateCommand.Parameters.AddWithValue("name", databaseName);
        await terminateCommand.ExecuteNonQueryAsync(cancellationToken);

        await using var dropCommand = connection.CreateCommand();
        dropCommand.CommandText = $"""DROP DATABASE IF EXISTS "{databaseName}" """;
        await dropCommand.ExecuteNonQueryAsync(cancellationToken);

        await using var createCommand = connection.CreateCommand();
        createCommand.CommandText = $"""CREATE DATABASE "{databaseName}" """;
        await createCommand.ExecuteNonQueryAsync(cancellationToken);
    }
}
