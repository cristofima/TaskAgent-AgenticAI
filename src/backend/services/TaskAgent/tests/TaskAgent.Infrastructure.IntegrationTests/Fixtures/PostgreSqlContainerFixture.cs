using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TaskAgent.Infrastructure.Data;
using Testcontainers.PostgreSql;

namespace TaskAgent.Infrastructure.IntegrationTests.Fixtures;

/// <summary>
/// Shared PostgreSQL container fixture for xUnit Collection.
/// This fixture starts a single PostgreSQL container that is shared across all tests in the collection.
/// </summary>
public class PostgreSqlContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public string ConnectionString { get; private set; } = string.Empty;

    public PostgreSqlContainerFixture()
    {
        // Use multiple combined wait strategies for maximum reliability
        _container = new PostgreSqlBuilder(image: "postgres:16-alpine")
            .WithDatabase("taskagent_integration_test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .WithWaitStrategy(Wait.ForUnixContainer()
                // 1. Verify PostgreSQL port is listening inside container
                .UntilInternalTcpPortIsAvailable(5432)
                // 2. Execute pg_isready health check command
                .UntilCommandIsCompleted("pg_isready", "-U", "test_user")
                // 3. Wait for PostgreSQL ready message in logs
                .UntilMessageIsLogged("database system is ready to accept connections")
                // 4. Verify actual database connection (most reliable check)
                .UntilDatabaseIsAvailable(NpgsqlFactory.Instance, o => o
                    .WithTimeout(TimeSpan.FromMinutes(1))    // PostgreSQL starts faster than SQL Server
                    .WithInterval(TimeSpan.FromSeconds(1)))  // Check every second
            )
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        // Create initial database schema
        await using ConversationDbContext context = CreateDbContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    /// <summary>
    /// Creates a new DbContext instance connected to the test container.
    /// </summary>
    public ConversationDbContext CreateDbContext()
    {
        DbContextOptions<ConversationDbContext> options = new DbContextOptionsBuilder<ConversationDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new ConversationDbContext(options);
    }
}

/// <summary>
/// xUnit Collection Definition for PostgreSQL tests.
/// Tests marked with [Collection("PostgreSql")] will share the same container instance.
/// </summary>
[CollectionDefinition("PostgreSql")]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class PostgreSqlCollection : ICollectionFixture<PostgreSqlContainerFixture>
#pragma warning restore CA1711
{
}
