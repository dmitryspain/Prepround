using Microsoft.EntityFrameworkCore;
using Npgsql;
using Prepround.Infrastructure;
using Testcontainers.PostgreSql;

namespace Prepround.Tests.Infrastructure;

/// <summary>
/// One throwaway Postgres (Testcontainers, the same image as docker-compose) for the whole collection.
/// Every test takes its own database from it, so tests never see each other's rows.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18-alpine").Build();

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();

    /// <summary>A fresh empty database; with <paramref name="migrate"/> it gets every migration, like after <c>ef database update</c>.</summary>
    public async Task<string> CreateDatabaseAsync(bool migrate = true)
    {
        var ct = TestContext.Current.CancellationToken;
        var name = $"t_{Guid.NewGuid():N}";

        await using (var connection = new NpgsqlConnection(_container.GetConnectionString()))
        {
            await connection.OpenAsync(ct);
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", connection);
            await command.ExecuteNonQueryAsync(ct);
        }

        var connectionString = new NpgsqlConnectionStringBuilder(_container.GetConnectionString()) { Database = name }.ConnectionString;
        if (migrate)
        {
            await using var context = CreateContext(connectionString);
            await context.Database.MigrateAsync(ct);
        }

        return connectionString;
    }

    public static PreproundContext CreateContext(string connectionString) =>
        new(new DbContextOptionsBuilder<PreproundContext>().UseNpgsql(connectionString).Options);

    public static async Task ExecuteAsync(string connectionString, string sql)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(ct);
    }

    /// <summary>
    /// Every row of every catalog table as JSON, ordered by key: equal snapshots mean that not a single value changed
    /// (row counts, IsActive, UpdatedAt and the rest).
    /// </summary>
    public static async Task<IReadOnlyList<string>> SnapshotAsync(string connectionString)
    {
        var ct = TestContext.Current.CancellationToken;
        var rows = new List<string>();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);

        foreach (var table in new[]
                 { "Fields", "Levels", "Directions", "Stacks", "StackTopics", "Topics", "Questions", "QuestionTranslations" })
        {
            await using var command = new NpgsqlCommand(
                $"SELECT '{table} ' || row_to_json(t)::text FROM \"{table}\" t ORDER BY row_to_json(t)::text", connection);
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                rows.Add(reader.GetString(0));
            }
        }

        return rows;
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
