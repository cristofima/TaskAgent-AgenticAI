using DotNet.Testcontainers.Builders;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TaskAgent.Infrastructure.Data;
using Testcontainers.MsSql;

namespace TaskAgent.Infrastructure.IntegrationTests.Fixtures;

/// <summary>
/// Shared SQL Server container fixture for xUnit Collection.
/// This fixture starts a single SQL Server container that is shared across all tests in the collection.
/// </summary>
public class SqlServerContainerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container;

    public string ConnectionString { get; private set; } = string.Empty;

    public SqlServerContainerFixture()
    {
        // Use multiple combined wait strategies for maximum reliability
        _container = new MsSqlBuilder(image: "mcr.microsoft.com/mssql/server:2022-latest")
            .WithWaitStrategy(Wait.ForUnixContainer()
                // 1. Verify SQL Server port is listening inside container
                .UntilInternalTcpPortIsAvailable(1433)
                // 2. Wait for SQL Server ready message in logs
                .UntilMessageIsLogged("SQL Server is now ready for client connections")
                // 3. Verify actual database connection (most reliable check)
                .UntilDatabaseIsAvailable(SqlClientFactory.Instance, o => o
                    .WithTimeout(TimeSpan.FromMinutes(2))    // SQL Server can take time to initialize
                    .WithInterval(TimeSpan.FromSeconds(2)))  // Check every 2 seconds
            )
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        // Create initial database schema
        await using TaskDbContext context = CreateDbContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    /// <summary>
    /// Creates a new DbContext instance connected to the test container.
    /// </summary>
    public TaskDbContext CreateDbContext()
    {
        DbContextOptions<TaskDbContext> options = new DbContextOptionsBuilder<TaskDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new TaskDbContext(options);
    }
}

/// <summary>
/// xUnit Collection Definition for SQL Server tests.
/// Tests marked with [Collection("SqlServer")] will share the same container instance.
/// </summary>
[CollectionDefinition("SqlServer")]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class SqlServerCollection : ICollectionFixture<SqlServerContainerFixture>
#pragma warning restore CA1711
{
}
