# GitHub Copilot Custom Instructions - C#/.NET Integration & Functional Tests Generation

These instructions guide GitHub Copilot in generating clean, maintainable, and effective **integration tests** and **functional tests** for C#/.NET projects using Testcontainers for database testing and WebApplicationFactory for API testing.

> **Note**: For unit tests (mocking, FluentAssertions, NSubstitute), see `dotnet-unit-tests-generation-instructions.md`.

---

## Table of Contents

1. **[Part I: Fundamentals](#part-i-fundamentals)** - Terminology, packages, project structure
2. **[Part II: Integration Tests](#part-ii-integration-tests-infrastructure-layer)** - Testcontainers, Base Classes, Repository testing
3. **[Part III: Functional Tests](#part-iii-functional-tests-webapi-layer)** - WebApplicationFactory, HTTP testing, Authentication
4. **[Part IV: Shared Patterns](#part-iv-shared-patterns)** - Database seeding, naming conventions, anti-patterns

---

# Part I: Fundamentals

This section covers the foundational concepts, terminology, and shared configuration for both integration and functional tests.

---

## Test Types Terminology (Microsoft Official)

According to Microsoft Learn documentation, there are clear distinctions:

| Test Type | What It Tests | Tools | Perspective | Project Suffix |
|-----------|---------------|-------|-------------|----------------|
| **Unit Tests** | Isolated logic, no dependencies | Mocks/Fakes (NSubstitute) | Internal code | `.UnitTests` |
| **Integration Tests** | Components + Infrastructure (DB, File System) | Testcontainers, Real databases | Developer | `.IntegrationTests` |
| **Functional Tests** | Full system stack (HTTP pipeline, middleware) | WebApplicationFactory + TestServer | User/End-to-end | `.FunctionalTests` |

> **Key Insight**: Integration tests verify that components work with real infrastructure (repositories with real DB). Functional tests verify the entire HTTP request/response cycle through WebApplicationFactory.

---

## Important Note

**DO NOT create `.md` documentation files with every prompt unless explicitly requested.**

## Using Microsoft Learn MCP

**Always use the Microsoft Learn MCP (Model Context Protocol) to consult up-to-date information** about:

- ASP.NET Core integration testing best practices
- WebApplicationFactory patterns
- Entity Framework Core testing strategies
- Testcontainers .NET documentation

**When to use Microsoft Learn MCP:**

- Before implementing new integration test patterns
- When configuring WebApplicationFactory
- To verify Testcontainers API usage
- To find official Microsoft integration test examples

**Available tools:**

- `microsoft_docs_search` - Search official Microsoft documentation
- `microsoft_code_sample_search` - Find official code examples
- `microsoft_docs_fetch` - Get complete documentation pages

---

## Integration Testing Framework Stack

### Recommended Packages

| Package | Version | Purpose |
|---------|---------|---------|
| **xUnit** | 2.9.x+ | Testing framework (Microsoft recommended) |
| **xunit.runner.visualstudio** | 3.x+ | Visual Studio/CLI test runner |
| **Microsoft.NET.Test.Sdk** | 17.x+ | Test SDK for .NET |
| **FluentAssertions** | 8.x+ | Readable assertion library |
| **Testcontainers.MsSql** | 4.x+ | SQL Server container |
| **Testcontainers.PostgreSql** | 4.x+ | PostgreSQL container |
| **Microsoft.AspNetCore.Mvc.Testing** | 10.x+ | WebApplicationFactory for API tests |
| **Microsoft.EntityFrameworkCore.Sqlite** | 10.x+ | SQLite for in-memory database tests |

### NuGet Package Configuration (Central Package Management)

```xml
<!-- Directory.Packages.props -->
<ItemGroup>
  <!-- Testing Framework -->
  <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
  <PackageVersion Include="xunit" Version="2.9.3" />
  <PackageVersion Include="xunit.runner.visualstudio" Version="3.1.4" />
  <PackageVersion Include="coverlet.collector" Version="6.0.4" />
  <!-- Testing Libraries -->
  <PackageVersion Include="FluentAssertions" Version="8.2.0" />
  <!-- Integration Testing - Testcontainers -->
  <PackageVersion Include="Testcontainers.MsSql" Version="4.4.0" />
  <PackageVersion Include="Testcontainers.PostgreSql" Version="4.4.0" />
  <!-- Integration Testing - WebApplicationFactory -->
  <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
  <!-- Integration Testing - SQLite (alternative to Testcontainers) -->
  <PackageVersion Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.0" />
</ItemGroup>
```

---

## Test Project Structure

### Project Layout

```
tests/
├── TaskAgent.Infrastructure.IntegrationTests/   # Repository + DB tests (Testcontainers)
│   ├── Fixtures/                                 # xUnit Collection Fixtures (recommended)
│   │   ├── SqlServerContainerFixture.cs          # SQL Server container lifecycle
│   │   ├── PostgreSqlContainerFixture.cs         # PostgreSQL container lifecycle
│   │   └── DatabaseCollections.cs                # Collection definitions
│   ├── Repositories/
│   │   └── TaskRepositoryTests.cs                # SQL Server repository tests
│   ├── Services/
│   │   └── ConversationServiceTests.cs           # PostgreSQL service tests
│   ├── GlobalUsings.cs
│   └── TaskAgent.Infrastructure.IntegrationTests.csproj
│
└── TaskAgent.WebApi.FunctionalTests/             # HTTP API tests (WebApplicationFactory)
    ├── Fixtures/
    │   └── CustomWebApplicationFactory.cs
    ├── Controllers/
    │   ├── TaskControllerTests.cs
    │   └── ChatControllerTests.cs
    ├── Endpoints/
    │   └── AgentEndpointTests.cs
    ├── GlobalUsings.cs
    └── TaskAgent.WebApi.FunctionalTests.csproj
```

> **Note**: We use **xUnit Collection Fixtures** pattern (not base class inheritance) for better isolation and cleaner test organization. See [xUnit Fixture Patterns](#xunit-fixture-patterns-recommended) for implementation details.

### Naming Convention for Test Projects

| Type | Suffix | Example | Description |
|------|--------|---------|-------------|
| Unit Tests | `.UnitTests` | `TaskAgent.Domain.UnitTests` | Isolated logic with mocks |
| Integration Tests | `.IntegrationTests` | `TaskAgent.Infrastructure.IntegrationTests` | Repository + real DB (Testcontainers) |
| Functional Tests | `.FunctionalTests` | `TaskAgent.WebApi.FunctionalTests` | HTTP API + WebApplicationFactory |

---

## Integration Test Project Configuration

### Integration Test Project (.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <!-- Testing Framework -->
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="coverlet.collector" />
    <!-- Testing Libraries -->
    <PackageReference Include="FluentAssertions" />
    <!-- Integration Testing -->
    <PackageReference Include="Testcontainers.MsSql" />
    <PackageReference Include="Testcontainers.PostgreSql" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\TaskAgent.WebApi\TaskAgent.WebApi.csproj" />
  </ItemGroup>

</Project>
```

**Important**: Use `Microsoft.NET.Sdk.Web` for WebApplicationFactory tests.

### Global Usings

```csharp
// GlobalUsings.cs
global using DotNet.Testcontainers.Builders;
global using FluentAssertions;
global using Microsoft.AspNetCore.Mvc.Testing;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Testcontainers.MsSql;
global using Testcontainers.PostgreSql;
global using Xunit;
```

**Note**: `DotNet.Testcontainers.Builders` is included via `Testcontainers.MsSql` or `Testcontainers.PostgreSql` packages and provides `Wait.ForUnixContainer()` for wait strategies.

---

# Part II: Integration Tests (Infrastructure Layer)

Integration tests verify that your **infrastructure components work correctly with real databases**. They test repositories, EF Core configurations, and data access patterns using Testcontainers.

**When to use Integration Tests:**
- Testing repository implementations
- Verifying EF Core queries and configurations
- Testing database migrations
- Validating transaction behavior

---

## Testcontainers Patterns

### Wait Strategy Options

Testcontainers provides multiple wait strategies. Choose based on your needs:

```csharp
// Option 1: Port availability (basic, fast)
.WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))

// Option 2: Command completion (more reliable for databases)
.WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready"))

// Option 3: With timeout configuration
.WithWaitStrategy(Wait.ForUnixContainer()
    .UntilCommandIsCompleted("pg_isready", "--host", "localhost")
    .WithTimeout(TimeSpan.FromMinutes(2)))

// Option 4: Chain multiple strategies
.WithWaitStrategy(Wait.ForUnixContainer()
    .UntilPortIsAvailable(5432)
    .UntilCommandIsCompleted("pg_isready"))
```

### IAsyncLifetime for Container Lifecycle

Testcontainers uses `IAsyncLifetime` to manage container startup and cleanup:

```csharp
public sealed class SqlServerContainerFixture : IAsyncLifetime
{
    public MsSqlContainer Container { get; }

    public string ConnectionString => Container.GetConnectionString();

    public SqlServerContainerFixture()
    {
        Container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("Test123!")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("SA_PASSWORD", "Test123!")
            .WithPortBinding(0, 1433) // Random host port for isolation
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
            .Build();
    }

    public Task InitializeAsync()
    {
        return Container.StartAsync();
    }

    public Task DisposeAsync()
    {
        return Container.DisposeAsync().AsTask();
    }
}
```

### PostgreSQL Container Fixture

```csharp
public sealed class PostgreSqlContainerFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; }

    public string ConnectionString => Container.GetConnectionString();

    public PostgreSqlContainerFixture()
    {
        Container = new PostgreSqlBuilder()
            .WithImage("postgres:15.6-alpine")
            .WithDatabase("testdb")
            .WithUsername("testuser")
            .WithPassword("Test123!")
            .WithPortBinding(0, 5432) // Random host port for isolation
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();
    }

    public Task InitializeAsync()
    {
        return Container.StartAsync();
    }

    public Task DisposeAsync()
    {
        return Container.DisposeAsync().AsTask();
    }
}
```

---

## xUnit Fixture Patterns (Recommended)

> **Recommended**: Use xUnit Collection Fixtures for cleaner test organization, better isolation, and avoiding tight coupling to base classes. This pattern is simpler and more maintainable for most scenarios.

### IClassFixture - Single Class

Use `IClassFixture<T>` when the container should be shared across all tests **in a single test class**:

```csharp
public class TaskRepositoryTests : IClassFixture<SqlServerContainerFixture>
{
    private readonly SqlServerContainerFixture _fixture;
    private readonly TaskDbContext _context;

    public TaskRepositoryTests(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
        
        var options = new DbContextOptionsBuilder<TaskDbContext>()
            .UseSqlServer(_fixture.ConnectionString)
            .Options;
            
        _context = new TaskDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task AddAsync_WithValidTask_PersistsToDatabase()
    {
        // Arrange
        var repository = new TaskRepository(_context);
        var task = TaskItem.Create("Integration Test Task", "Description", TaskPriority.High);

        // Act
        await repository.AddAsync(task);
        await _context.SaveChangesAsync();

        // Assert
        TaskItem? savedTask = await _context.Tasks.FirstOrDefaultAsync(t => t.Title == "Integration Test Task");
        savedTask.Should().NotBeNull();
        savedTask!.Priority.Should().Be(TaskPriority.High);
    }
}
```

### ICollectionFixture - Multiple Classes (Recommended)

Use `ICollectionFixture<T>` when the container should be shared across **multiple test classes**. This is the **recommended pattern** for this project:

```csharp
// Step 1: Define the collection
[CollectionDefinition("SqlServer")]
public class SqlServerCollection : ICollectionFixture<SqlServerContainerFixture>
{
    // This class has no code. Its purpose is to be the place to apply
    // [CollectionDefinition] and implement ICollectionFixture<T>.
}

[CollectionDefinition("PostgreSql")]
public class PostgreSqlCollection : ICollectionFixture<PostgreSqlContainerFixture>
{
    // Same pattern for PostgreSQL
}

// Step 2: Use in test classes with [Collection] attribute
[Collection("SqlServer")]
public class TaskRepositoryTests
{
    private readonly SqlServerContainerFixture _fixture;

    public TaskRepositoryTests(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllTasks()
    {
        // Test implementation using _fixture.ConnectionString
    }
}

[Collection("PostgreSql")]
public class ConversationServiceTests
{
    private readonly PostgreSqlContainerFixture _fixture;

    public ConversationServiceTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetByThreadIdAsync_ReturnsConversation()
    {
        // Test implementation using _fixture.ConnectionString
    }
}
```

### Complete Implementation Example

Here's a complete example of the recommended pattern used in this project:

**Fixtures/SqlServerContainerFixture.cs**
```csharp
using DotNet.Testcontainers.Builders;
using Testcontainers.MsSql;

namespace TaskAgent.Infrastructure.IntegrationTests.Fixtures;

public sealed class SqlServerContainerFixture : IAsyncLifetime
{
    public MsSqlContainer Container { get; }
    
    public string ConnectionString => Container.GetConnectionString();

    public SqlServerContainerFixture()
    {
        Container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("YourStrong@Passw0rd!")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithPortBinding(0, 1433)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
            .Build();
    }

    public Task InitializeAsync() => Container.StartAsync();
    
    public Task DisposeAsync() => Container.DisposeAsync().AsTask();
}
```

**Fixtures/DatabaseCollections.cs**
```csharp
namespace TaskAgent.Infrastructure.IntegrationTests.Fixtures;

[CollectionDefinition("SqlServer")]
public class SqlServerCollection : ICollectionFixture<SqlServerContainerFixture>
{
}

[CollectionDefinition("PostgreSql")]
public class PostgreSqlCollection : ICollectionFixture<PostgreSqlContainerFixture>
{
}
```

**Repositories/TaskRepositoryTests.cs**
```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskAgent.Domain.Entities;
using TaskAgent.Domain.Enums;
using TaskAgent.Infrastructure.Data;
using TaskAgent.Infrastructure.IntegrationTests.Fixtures;
using TaskAgent.Infrastructure.Repositories;

namespace TaskAgent.Infrastructure.IntegrationTests.Repositories;

[Collection("SqlServer")]
public class TaskRepositoryTests : IAsyncLifetime
{
    private readonly SqlServerContainerFixture _fixture;
    private TaskDbContext _context = null!;
    private TaskRepository _repository = null!;

    public TaskRepositoryTests(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<TaskDbContext>()
            .UseSqlServer(_fixture.ConnectionString)
            .Options;

        _context = new TaskDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        _repository = new TaskRepository(_context);
    }

    public async Task DisposeAsync()
    {
        // Clean up data between tests for isolation
        await _context.Tasks.ExecuteDeleteAsync();
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_ShouldPersistTask()
    {
        // Arrange
        var task = TaskItem.Create("Test Task", "Description", TaskPriority.High);

        // Act
        await _repository.AddAsync(task);

        // Assert
        var savedTask = await _context.Tasks.FirstOrDefaultAsync(t => t.Title == "Test Task");
        savedTask.Should().NotBeNull();
        savedTask!.Priority.Should().Be(TaskPriority.High);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ShouldReturnTask()
    {
        // Arrange
        var task = TaskItem.Create("Find Me", "Description", TaskPriority.Medium);
        await _repository.AddAsync(task);

        // Act
        var result = await _repository.GetByIdAsync(task.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Find Me");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(99999);

        // Assert
        result.Should().BeNull();
    }
}
```

---

## Base Class Pattern (Alternative Reference)

> **Note**: This pattern is provided as a reference for complex scenarios requiring full ServiceCollection control. For most use cases, prefer the [xUnit Fixture Patterns](#xunit-fixture-patterns-recommended) above.

The base class pattern can be useful when you need to:
- Register multiple services with complex DI configurations
- Share initialization logic across many test classes
- Have complete control over the ServiceProvider lifecycle

<details>
<summary><strong>Click to expand Base Class examples</strong></summary>

### SQL Server Base Class Example

```csharp
using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.MsSql;

namespace TaskAgent.Infrastructure.IntegrationTests.Common;

public abstract class BaseSqlServerInfrastructureTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer;
    private ServiceProvider? _serviceProvider;

    protected BaseSqlServerInfrastructureTests()
    {
        _sqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("Test123!")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithPortBinding(0, 1433)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
            .Build();
    }

    protected ServiceProvider ServiceProvider =>
        _serviceProvider ?? throw new InvalidOperationException("Service provider not initialized");

    protected T GetService<T>() where T : notnull =>
        ServiceProvider.GetRequiredService<T>();

    protected TaskDbContext GetDbContext() => GetService<TaskDbContext>();

    protected IServiceScope CreateScope() => ServiceProvider.CreateScope();

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _sqlContainer.GetConnectionString()
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddDbContext<TaskDbContext>(options =>
        {
            options.UseSqlServer(_sqlContainer.GetConnectionString());
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging();
        });

        services.AddInfrastructureServices(configuration);
        _serviceProvider = services.BuildServiceProvider();

        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider != null)
            await _serviceProvider.DisposeAsync();
        await _sqlContainer.DisposeAsync();
    }
}
```

### PostgreSQL Base Class Example

```csharp
public abstract class BasePostgreSqlInfrastructureTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _sqlContainer;
    private ServiceProvider? _serviceProvider;

    protected BasePostgreSqlInfrastructureTests()
    {
        _sqlContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15.6-alpine")
            .WithUsername("testuser")
            .WithPassword("Test123!")
            .WithPortBinding(0, 5432)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();
    }

    // Similar implementation to SQL Server base class...
}
```

### Using Base Class in Tests

```csharp
public class TaskRepositoryTests : BaseSqlServerInfrastructureTests
{
    private ITaskRepository GetTaskRepository() => GetService<ITaskRepository>();

    [Fact]
    public async Task GetByIdAsync_ShouldReturnTask_WhenTaskExists()
    {
        var task = TaskItem.Create("Test Task", "Description", TaskPriority.High);
        var taskRepository = GetTaskRepository();
        await taskRepository.AddAsync(task);

        var result = await taskRepository.GetByIdAsync(task.Id);

        Assert.NotNull(result);
        Assert.Equal(task.Title, result.Title);
    }
}
```

</details>

---

# Part III: Functional Tests (WebApi Layer)

Functional tests verify that your **HTTP API works correctly from the user's perspective**. They test the full request/response cycle including controllers, middleware, filters, routing, and authentication.

**When to use Functional Tests:**
- Testing API endpoints and HTTP responses
- Verifying authentication and authorization
- Testing middleware behavior
- End-to-end user workflows through HTTP

---

## WebApplicationFactory Patterns

> **Microsoft Definition**: "Functional tests are written from the perspective of the user, and verify the correctness of the system based on its requirements." They test the full HTTP request/response cycle including middleware, filters, and routing.

### When to Use Functional Tests vs Integration Tests

| Aspect | Integration Tests | Functional Tests |
|--------|-------------------|------------------|
| **Scope** | Infrastructure layer (repositories, EF Core) | Full HTTP pipeline (controllers, middleware) |
| **Tools** | Testcontainers, direct DbContext | WebApplicationFactory + HttpClient |
| **Perspective** | Developer (components work together) | User (system behaves correctly) |
| **Project** | `.Infrastructure.IntegrationTests` | `.WebApi.FunctionalTests` |

### Custom WebApplicationFactory

`WebApplicationFactory<TEntryPoint>` creates a `TestServer` for functional testing ASP.NET Core applications:

```csharp
public class CustomWebApplicationFactory<TProgram> 
    : WebApplicationFactory<TProgram> where TProgram : class
{
    private readonly MsSqlContainer _sqlServerContainer;
    private readonly PostgreSqlContainer _postgresContainer;

    public CustomWebApplicationFactory()
    {
        _sqlServerContainer = new MsSqlBuilder()
            .WithPassword("Strong_password_123!")
            .Build();

        _postgresContainer = new PostgreSqlBuilder()
            .WithDatabase("conversations")
            .WithUsername("testuser")
            .WithPassword("testpassword")
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registrations
            ServiceDescriptor? taskDbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<TaskDbContext>));
            if (taskDbDescriptor != null)
                services.Remove(taskDbDescriptor);

            ServiceDescriptor? conversationDbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ConversationDbContext>));
            if (conversationDbDescriptor != null)
                services.Remove(conversationDbDescriptor);

            // Add test containers
            services.AddDbContext<TaskDbContext>(options =>
                options.UseSqlServer(_sqlServerContainer.GetConnectionString()));

            services.AddDbContext<ConversationDbContext>(options =>
                options.UseNpgsql(_postgresContainer.GetConnectionString()));
        });

        builder.UseEnvironment("Testing");
    }

    public async Task InitializeContainersAsync()
    {
        await _sqlServerContainer.StartAsync();
        await _postgresContainer.StartAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await _sqlServerContainer.DisposeAsync();
        await _postgresContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
```

### Writing Functional Tests

```csharp
public class TaskControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TaskControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeContainersAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetTasks_ReturnsSuccessStatusCode()
    {
        // Arrange & Act
        HttpResponseMessage response = await _client.GetAsync("/api/tasks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateTask_WithValidData_ReturnsCreated()
    {
        // Arrange
        var createRequest = new
        {
            Title = "Functional Test Task",
            Description = "Created via functional test",
            Priority = "High"
        };
        var content = new StringContent(
            JsonSerializer.Serialize(createRequest),
            Encoding.UTF8,
            "application/json");

        // Act
        HttpResponseMessage response = await _client.PostAsync("/api/tasks", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
```

---

# Part IV: Shared Patterns

This section covers patterns and practices that apply to **both integration and functional tests**.

---

## Database Seeding and Cleanup

### Seeding Test Data

```csharp
public static class TestDatabaseSeeder
{
    public static async Task SeedTasksAsync(TaskDbContext context)
    {
        if (await context.Tasks.AnyAsync())
            return;

        var tasks = new List<TaskItem>
        {
            TaskItem.Create("Test Task 1", "Description 1", TaskPriority.High),
            TaskItem.Create("Test Task 2", "Description 2", TaskPriority.Medium),
            TaskItem.Create("Test Task 3", "Description 3", TaskPriority.Low)
        };

        context.Tasks.AddRange(tasks);
        await context.SaveChangesAsync();
    }

    public static async Task ClearTasksAsync(TaskDbContext context)
    {
        context.Tasks.RemoveRange(context.Tasks);
        await context.SaveChangesAsync();
    }
}
```

### Using Seeder in Tests

```csharp
public class TaskRepositoryTests : IClassFixture<SqlServerContainerFixture>, IAsyncLifetime
{
    private readonly SqlServerContainerFixture _fixture;
    private TaskDbContext _context = null!;

    public TaskRepositoryTests(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<TaskDbContext>()
            .UseSqlServer(_fixture.ConnectionString)
            .Options;

        _context = new TaskDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        await TestDatabaseSeeder.SeedTasksAsync(_context);
    }

    public async Task DisposeAsync()
    {
        await TestDatabaseSeeder.ClearTasksAsync(_context);
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsSeededTasks()
    {
        // Arrange
        var repository = new TaskRepository(_context);

        // Act
        IEnumerable<TaskItem> tasks = await repository.GetAllAsync();

        // Assert
        tasks.Should().HaveCount(3);
    }
}
```

---

## SQLite In-Memory Alternative

> **Part II Context**: This is an alternative to Testcontainers for integration tests when you need faster execution and don't require database-specific features.

For faster tests that don't require specific database features, use SQLite:

```csharp
public class SqliteTaskRepositoryTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly TaskDbContext _context;

    public SqliteTaskRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TaskDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new TaskDbContext(options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task AddAsync_WithValidTask_PersistsToDatabase()
    {
        // Arrange
        var repository = new TaskRepository(_context);
        var task = TaskItem.Create("SQLite Test Task", "Description", TaskPriority.High);

        // Act
        await repository.AddAsync(task);
        await _context.SaveChangesAsync();

        // Assert
        TaskItem? savedTask = await _context.Tasks.FirstOrDefaultAsync();
        savedTask.Should().NotBeNull();
    }
}
```

**Microsoft Recommendation**: SQLite is the recommended choice for in-memory testing over EF Core's InMemory provider.

---

## ConfigureTestServices for Service Mocking

> **Part III Context**: This pattern is specific to functional tests with WebApplicationFactory, allowing you to replace services without modifying the SUT.

Replace services in functional tests without modifying the SUT:

```csharp
[Fact]
public async Task GetTask_WithMockedService_ReturnsExpectedData()
{
    // Arrange
    var mockTaskService = Substitute.For<ITaskService>();
    mockTaskService.GetByIdAsync(1).Returns(
        TaskItem.Create("Mocked Task", "Description", TaskPriority.High));

    var client = _factory.WithWebHostBuilder(builder =>
    {
        builder.ConfigureTestServices(services =>
        {
            services.AddScoped(_ => mockTaskService);
        });
    }).CreateClient();

    // Act
    HttpResponseMessage response = await client.GetAsync("/api/tasks/1");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    
    string content = await response.Content.ReadAsStringAsync();
    content.Should().Contain("Mocked Task");
}
```

---

## Mock Authentication

> **Part III Context**: This pattern is specific to functional tests for testing authenticated API endpoints.

For testing authenticated endpoints:

```csharp
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] 
        { 
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

// Usage in tests
[Fact]
public async Task SecureEndpoint_WithAuthenticatedUser_ReturnsOk()
{
    // Arrange
    var client = _factory.WithWebHostBuilder(builder =>
    {
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(defaultScheme: "TestScheme")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    "TestScheme", options => { });
        });
    }).CreateClient();

    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue(scheme: "TestScheme");

    // Act
    HttpResponseMessage response = await client.GetAsync("/api/secure");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

---

## What to Test Where

> **Decision Guide**: Use this section to determine which type of test to write for a given scenario.

### Integration Tests (Infrastructure Layer)

Test repository and data access with real databases:

- ✅ Repository implementations with real database
- ✅ Database migrations and schema
- ✅ Data persistence and retrieval
- ✅ Transaction behavior
- ✅ EF Core queries and configurations
- ✅ External service integrations (non-HTTP)

### Functional Tests (WebApi Layer)

Test HTTP endpoints with full pipeline:

- ✅ API endpoints (controllers, minimal APIs)
- ✅ Authentication and authorization flows
- ✅ Request/Response serialization
- ✅ Middleware behavior
- ✅ Routing and URL generation
- ✅ End-to-end user workflows

### Comparison Table

| Aspect | Integration Tests | Functional Tests |
|--------|-------------------|------------------|
| **Dependencies** | Real DB (Testcontainers) | Full app + TestServer |
| **Speed** | Slower (seconds) | Slowest (full HTTP) |
| **Isolation** | Per-test class | Per-test or shared |
| **Database** | Yes (Testcontainers/SQLite) | Yes (via WebApplicationFactory) |
| **HTTP Pipeline** | No | Yes (middleware, filters) |
| **Project Suffix** | `.IntegrationTests` | `.FunctionalTests` |

---

## AAA Pattern for Integration Tests

Integration tests still follow the AAA pattern:

```csharp
[Fact]
public async Task CreateAndRetrieveTask_FullWorkflow_Succeeds()
{
    // Arrange
    var repository = new TaskRepository(_context);
    var task = TaskItem.Create("Full Workflow Task", "Testing end-to-end", TaskPriority.High);

    // Act
    await repository.AddAsync(task);
    await _context.SaveChangesAsync();
    
    TaskItem? retrievedTask = await repository.GetByIdAsync(task.Id);

    // Assert
    retrievedTask.Should().NotBeNull();
    retrievedTask!.Title.Should().Be("Full Workflow Task");
    retrievedTask.Status.Should().Be(TaskStatus.Pending);
}
```

---

## Test Naming Convention

Same convention as unit tests:

```
MethodName_Scenario_ExpectedBehavior
```

### Examples

```csharp
// Repository tests
[Fact]
public async Task GetByIdAsync_WithExistingId_ReturnsTask() { }

[Fact]
public async Task GetByIdAsync_WithNonExistingId_ReturnsNull() { }

// API tests
[Fact]
public async Task PostTask_WithValidData_ReturnsCreated() { }

[Fact]
public async Task PostTask_WithInvalidData_ReturnsBadRequest() { }

// Authentication tests
[Fact]
public async Task SecurePage_WithoutAuthentication_RedirectsToLogin() { }

[Fact]
public async Task SecurePage_WithValidAuthentication_ReturnsOk() { }
```

---

## Parallel Test Execution

### Disable Parallelization for Database Tests

Create `xunit.runner.json` to control test parallelization:

```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeAssembly": false,
  "parallelizeTestCollections": false,
  "shadowCopy": false
}
```

### Or Use Collection to Group Related Tests

```csharp
[CollectionDefinition("Database collection", DisableParallelization = true)]
public class DatabaseCollection : ICollectionFixture<SqlServerContainerFixture>
{
}
```

---

## Common Anti-Patterns to Avoid

### ❌ Not Cleaning Up Test Data

```csharp
// ❌ BAD - No cleanup
[Fact]
public async Task Test1()
{
    await _context.Tasks.AddAsync(task);
    await _context.SaveChangesAsync();
    // No cleanup - affects other tests
}

// ✅ GOOD - Proper cleanup
public async Task DisposeAsync()
{
    await TestDatabaseSeeder.ClearTasksAsync(_context);
}
```

### ❌ Hardcoded Connection Strings

```csharp
// ❌ BAD
var options = new DbContextOptionsBuilder<TaskDbContext>()
    .UseSqlServer("Server=localhost;Database=Test;...")
    .Options;

// ✅ GOOD - Use container connection string
var options = new DbContextOptionsBuilder<TaskDbContext>()
    .UseSqlServer(_fixture.ConnectionString)
    .Options;
```

### ❌ Not Waiting for Container

```csharp
// ❌ BAD - Container might not be ready
public TaskRepositoryTests(SqlServerContainerFixture fixture)
{
    _fixture = fixture;
    // Container might still be starting!
}

// ✅ GOOD - Use IAsyncLifetime
public async Task InitializeAsync()
{
    await _fixture.Container.StartAsync();
    // Now container is ready
}
```

---

## Summary Checklist

### For Integration Tests (Infrastructure Layer with Testcontainers)

- [ ] **Base Class Pattern** - Use abstract base class with ServiceCollection for complex scenarios
- [ ] **IAsyncLifetime** - Proper container lifecycle management
- [ ] **Database Cleanup** - Clean state between tests (`ExecuteDeleteAsync` for efficiency)
- [ ] **AAA Pattern** - Clear Arrange, Act, Assert sections
- [ ] **Descriptive Names** - `MethodName_ShouldExpectedBehavior_WhenCondition`
- [ ] **FluentAssertions** - Readable assertion syntax
- [ ] **Test Isolation** - No shared mutable state
- [ ] **Container Images** - Use specific versions (`postgres:15.6-alpine`, `mssql/server:2022-latest`)
- [ ] **Connection Strings** - Always from container, never hardcoded
- [ ] **WaitStrategy** - Use `UntilPortIsAvailable(port)` or `UntilCommandIsCompleted("pg_isready")`
- [ ] **Random Port Binding** - Use `WithPortBinding(0, port)` for test isolation
- [ ] **Retry on Failure** - Configure `EnableRetryOnFailure` for DbContext options
- [ ] **Test Interfaces** - Create test implementations for shared interfaces (e.g., `TestCurrentUser`)

### For Functional Tests (WebApi Layer with WebApplicationFactory)

- [ ] **WebApplicationFactory** - Proper service replacement via `ConfigureTestServices`
- [ ] **IClassFixture** - Share factory across tests in a class
- [ ] **HttpClient** - Use `factory.CreateClient()` for HTTP requests
- [ ] **SDK.Web** - Use `Microsoft.NET.Sdk.Web` in test project
- [ ] **Test Environment** - Set via `builder.UseEnvironment("Testing")`
- [ ] **Mock Authentication** - Use `TestAuthHandler` for authenticated endpoints
- [ ] **xunit.runner.json** - Control parallelization for sequential tests

### Project Naming (Microsoft Official)

| Test Type | Project Suffix | Example |
|-----------|---------------|---------|
| Unit Tests | `.UnitTests` | `TaskAgent.Domain.UnitTests` |
| Integration Tests | `.IntegrationTests` | `TaskAgent.Infrastructure.IntegrationTests` |
| Functional Tests | `.FunctionalTests` | `TaskAgent.WebApi.FunctionalTests` |

---

_These guidelines ensure maintainable, reliable, and effective integration and functional tests following Microsoft and Testcontainers best practices. Patterns based on real-world production implementations._
