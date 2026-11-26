using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Voxa.WebApi.PostgresTests;

/// <summary>
/// A real PostgreSQL database for tests whose behaviour depends on the provider: unique indexes, LIKE case rules and
/// <c>timestamp with time zone</c> values, which the EF in-memory provider ignores. The database is created with the
/// application's migrations and dropped afterwards.
/// <para>
/// Set <see cref="ServerVariable"/> to a connection string to use an existing server (a fresh database is created on
/// it). Otherwise a throwaway <c>postgres:18</c> container is started, which needs Docker.
/// </para>
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    public const string ServerVariable = "VOXA_TEST_POSTGRES";

    private PostgreSqlContainer? _container;

    /// <summary>The test database's connection string.</summary>
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var server = Environment.GetEnvironmentVariable(ServerVariable);
        if (string.IsNullOrWhiteSpace(server))
        {
            _container = new PostgreSqlBuilder("postgres:18").Build();
            await _container.StartAsync();
            server = _container.GetConnectionString();
        }

        ConnectionString = new NpgsqlConnectionStringBuilder(server) { Database = $"voxa_test_{Guid.NewGuid():N}" }.ConnectionString;
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public VoxaContext CreateContext() =>
        new(new DbContextOptionsBuilder<VoxaContext>().UseNpgsql(ConnectionString).Options);

    public async Task DisposeAsync()
    {
        if (ConnectionString.Length > 0)
        {
            await using var context = CreateContext();
            await context.Database.EnsureDeletedAsync();
        }

        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "PostgreSQL";
}
