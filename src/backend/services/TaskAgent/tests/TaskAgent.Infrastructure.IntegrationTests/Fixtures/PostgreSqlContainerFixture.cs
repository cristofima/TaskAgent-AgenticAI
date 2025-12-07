using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
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
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("taskagent_integration_test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready", "-U", "test_user"))
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
