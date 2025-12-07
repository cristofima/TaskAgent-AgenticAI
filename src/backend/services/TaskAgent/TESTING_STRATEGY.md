# Testing Strategy - TaskAgent Backend

> ✅ **Implementation Status**: 132 tests implemented (December 2025)
> - Domain Unit Tests: 28 tests ✅
> - Application Unit Tests: 75 tests ✅
> - Infrastructure Integration Tests: 29 tests ✅
> - WebApi Functional Tests: 🚧 Planned

## Project Overview

| Aspect | Details |
|--------|---------|
| **Framework** | .NET 10.0 |
| **Architecture** | Clean Architecture (Domain → Application → Infrastructure → WebApi) |
| **ORM** | Entity Framework Core |
| **Databases** | SQL Server (Tasks), PostgreSQL (Conversations) |
| **AI Integration** | Microsoft.Agents.AI.OpenAI (preview) |
| **Observability** | OpenTelemetry, Serilog |

---

## Testing Framework Comparison

| Feature | **xUnit** | **NUnit** | **MSTest** | **TUnit** |
|---------|-----------|-----------|------------|-----------|
| **Microsoft Testing Platform** | ✅ v3+ | ✅ Runner | ✅ Native | ✅ Native |
| **Parallel Execution** | ✅ Native | ✅ Native | ✅ Native | ✅ Native |
| **Theory/Data-Driven** | ✅ `[Theory]` | ✅ `[TestCase]` | ✅ `[DataRow]` | ✅ `[Arguments]` |
| **Fixtures (Shared Context)** | ✅ `IClassFixture` | ✅ `[SetUp]` | ✅ `[TestInitialize]` | ✅ `[Before]` |
| **Collection Fixtures** | ✅ `ICollectionFixture` | ⚠️ Limited | ⚠️ Limited | ✅ Native |
| **Async Support** | ✅ Full | ✅ Full | ✅ Full | ✅ Full |
| **Community Adoption** | 🔥 High (ASP.NET Core default) | 🔥 High | 🟡 Medium | 🟢 Growing |
| **.NET 10 Support** | ✅ | ✅ | ✅ | ✅ |

---

## Recommendation: **xUnit + Testcontainers + NSubstitute + FluentAssertions**

### Why This Stack?

#### xUnit 2.9+ / xUnit v3
- ⚡ Default testing framework for ASP.NET Core projects
- ✅ Official Microsoft recommendation for .NET testing
- ✅ `IClassFixture` and `ICollectionFixture` for shared database contexts
- ✅ `IAsyncLifetime` for async setup/teardown (perfect for containers)
- ✅ Native parallelization with collection-based isolation

#### Testcontainers 3.x (.NET)
- 🐳 Spin up real SQL Server and PostgreSQL containers for integration tests
- ✅ No mock databases - test against real database engines
- ✅ Automatic cleanup after tests
- ✅ Pre-built modules: `Testcontainers.MsSql`, `Testcontainers.PostgreSql`
- ✅ Perfect for EF Core migrations testing

#### NSubstitute 5.x
- 🎭 Clean, fluent syntax for mocking interfaces
- ✅ No `Setup()` / `Verify()` boilerplate like Moq
- ✅ Full async support
- ✅ Type-safe argument matchers with `Arg.Any<T>()`

#### FluentAssertions 7.x
- ✅ Readable assertion syntax: `result.Should().BeTrue()`
- ✅ Collection assertions: `tasks.Should().HaveCount(3)`
- ✅ Object comparison: `actual.Should().BeEquivalentTo(expected)`
- ✅ Exception assertions: `action.Should().Throw<ArgumentException>()`

#### Bogus 35.x
- 🎲 Generate realistic fake test data
- ✅ Strongly typed with `Faker<T>` API
- ✅ Locale support (es, en, pt, etc.)
- ✅ Reproducible data with `Seed()`

---

## Test Categories and Layers

```
┌─────────────────────────────────────────────────────────────────┐
│                        E2E / Acceptance Tests                   │
│           (Full API with Testcontainers databases)              │
│                    WebApplicationFactory + Containers           │
├─────────────────────────────────────────────────────────────────┤
│                       Integration Tests                         │
│        (Repository + DbContext with Testcontainers)             │
│              Real SQL Server / PostgreSQL containers            │
├─────────────────────────────────────────────────────────────────┤
│                         Unit Tests                              │
│    (Domain entities, Application services, Functions)           │
│                    Mocked dependencies                          │
└─────────────────────────────────────────────────────────────────┘
```

### Test Project Structure

> **Naming Convention**: Per Microsoft best practices, test projects use explicit suffixes:
> - `.UnitTests` - For fast, isolated unit tests with mocked dependencies
> - `.IntegrationTests` - For tests requiring real infrastructure (Testcontainers)

```
tests/
├── TaskAgent.Domain.UnitTests/              # Unit tests for entities
│   ├── Entities/
│   │   └── TaskItemTests.cs
│   ├── Constants/
│   │   ├── TaskConstantsTests.cs
│   │   └── ValidationMessagesTests.cs
│   ├── GlobalUsings.cs
│   └── TaskAgent.Domain.UnitTests.csproj
│
├── TaskAgent.Application.UnitTests/         # Unit tests for functions/services
│   ├── Functions/
│   │   ├── CreateTaskTests.cs
│   │   ├── ListTasksTests.cs
│   │   ├── GetTaskDetailsTests.cs
│   │   ├── UpdateTaskTests.cs
│   │   ├── DeleteTaskTests.cs
│   │   └── GetTaskSummaryTests.cs
│   ├── Validators/
│   │   └── PaginationValidatorTests.cs
│   ├── GlobalUsings.cs
│   └── TaskAgent.Application.UnitTests.csproj
│
├── TaskAgent.Infrastructure.IntegrationTests/  # Integration tests with Testcontainers
│   ├── Fixtures/
│   │   ├── SqlServerContainerFixture.cs
│   │   ├── PostgreSqlContainerFixture.cs
│   │   └── DatabaseCollection.cs
│   ├── Repositories/
│   │   └── TaskRepositoryTests.cs
│   ├── Services/
│   │   └── ConversationServiceTests.cs
│   └── TaskAgent.Infrastructure.IntegrationTests.csproj
│
├── TaskAgent.WebApi.IntegrationTests/          # API integration tests
│   ├── Fixtures/
│   │   └── CustomWebApplicationFactory.cs
│   ├── Controllers/
│   │   ├── AgentControllerTests.cs
│   │   └── ConversationsControllerTests.cs
│   └── TaskAgent.WebApi.IntegrationTests.csproj
│
└── TaskAgent.Tests.Common/                     # Shared test utilities
    ├── Builders/
    │   └── TaskItemBuilder.cs
    ├── Fakers/
    │   └── TaskItemFaker.cs
    └── TaskAgent.Tests.Common.csproj
```

---

## Components to Test (Priority Matrix)

### 🔴 High Priority - Unit Tests

| File | Type | Complexity | Status | Tests |
|------|------|------------|--------|-------|
| `Domain/Entities/TaskItem.cs` | Entity | 🟢 Low | ✅ Done | 23 |
| `Domain/Constants/TaskConstants.cs` | Constants | 🟢 Low | ✅ Done | 2 |
| `Domain/Constants/ValidationMessages.cs` | Constants | 🟢 Low | ✅ Done | 3 |
| `Application/Functions/TaskFunctions.cs` | AI Functions | 🟠 Medium | ✅ Done | 75 |

### 🟡 Medium Priority - Integration Tests (Testcontainers)

| File | Type | Complexity | Status | Tests |
|------|------|------------|--------|-------|
| `Infrastructure/Repositories/TaskRepository.cs` | Repository | 🟠 Medium | ✅ Done | 13 |
| `Infrastructure/Services/ConversationService.cs` | Service | 🟠 Medium | ✅ Done | 16 |
| `Infrastructure/MessageStores/PostgresChatMessageStore.cs` | Store | 🔴 High | 🚧 Planned | - |
| `Infrastructure/Data/TaskDbContext.cs` | DbContext | 🟠 Medium | ✅ Covered | - |
| `Infrastructure/Data/ConversationDbContext.cs` | DbContext | 🟠 Medium | ✅ Covered | - |

### 🟢 Lower Priority - API Functional Tests

| Flow | Priority | Complexity | Description |
|------|----------|------------|-------------|
| `POST /api/agent/chat` | 🔴 High | 🔴 High | SSE streaming, thread persistence |
| `GET /api/conversations` | 🟡 Medium | 🟠 Medium | List conversations with pagination |
| `GET /api/conversations/{threadId}/messages` | 🟡 Medium | 🟠 Medium | Get conversation history |
| `DELETE /api/conversations/{threadId}` | 🟡 Medium | 🟢 Low | Soft delete conversation |

### 🟡 Planned - WebApi Unit Tests

| File | Type | Complexity | Status | Tests |
|------|------|------------|--------|-------|
| `WebApi/Constants/AgentConstants.cs` | Constants | 🟢 Low | 🚧 Planned | ~8 |
| `WebApi/Services/FunctionDescriptionProvider.cs` | Service | 🟠 Medium | 🚧 Planned | ~12 |
| `WebApi/Services/SseStreamingService.cs` | Service | 🟠 Medium | 🚧 Planned | ~10 |

#### Planned AgentConstants Tests

```csharp
public class AgentConstantsTests
{
    // SSE Event Types
    [Fact] public void EVENT_STATUS_UPDATE_ShouldBe_STATUS_UPDATE()
    [Fact] public void EVENT_THREAD_STATE_ShouldBe_THREAD_STATE()
    [Fact] public void EVENT_CONTENT_FILTER_ShouldBe_CONTENT_FILTER()
    [Fact] public void EVENT_STEP_STARTED_ShouldBe_STEP_STARTED()
    [Fact] public void EVENT_STEP_FINISHED_ShouldBe_STEP_FINISHED()
    
    // Generic Status Messages (non function-specific)
    [Fact] public void STATUS_LOADING_HISTORY_ShouldBeCorrectMessage()
    [Fact] public void STATUS_PROCESSING_REQUEST_ShouldBeCorrectMessage()
}
```

#### Planned FunctionDescriptionProvider Tests

```csharp
public class FunctionDescriptionProviderTests
{
    // Registration Tests
    [Fact] public void RegisterFunctionType_WithTaskFunctions_RegistersAllMethods()
    [Fact] public void RegisterFunctionType_WithNullType_ThrowsArgumentNullException()
    [Fact] public void RegisterFunctionType_CalledTwice_DoesNotDuplicate()
    
    // GetStatusMessage Tests
    [Fact] public void GetStatusMessage_CreateTaskAsync_ReturnsCreatingTask()
    [Fact] public void GetStatusMessage_ListTasksAsync_ReturnsListingTasks()
    [Fact] public void GetStatusMessage_GetTaskDetailsAsync_ReturnsGettingDetails()
    [Fact] public void GetStatusMessage_UpdateTaskAsync_ReturnsUpdatingTask()
    [Fact] public void GetStatusMessage_DeleteTaskAsync_ReturnsDeletingTask()
    [Fact] public void GetStatusMessage_GetTaskSummaryAsync_ReturnsGeneratingSummary()
    [Fact] public void GetStatusMessage_UnknownFunction_ReturnsProcessingRequest()
    
    // Caching Tests
    [Fact] public void GetStatusMessage_SameFunction_UsesCachedValue()
}
```

#### Planned SseStreamingService Tests

```csharp
public class SseStreamingServiceTests
{
    // STEP Lifecycle Events
    [Fact] public async Task SendStepStartedAsync_SendsCorrectJsonFormat()
    [Fact] public async Task SendStepFinishedAsync_SendsCorrectJsonFormat()
    
    // STATUS_UPDATE Events (dynamic from FunctionDescriptionProvider)
    [Fact] public async Task SendStatusUpdateAsync_SendsCorrectJsonFormat()
    [Fact] public async Task SendFunctionStatusAsync_UsesFunctionDescriptionProvider()
    
    // Content Filter Events
    [Fact] public async Task SendContentFilterAsync_SendsCorrectJsonFormat()
    
    // Thread State Events
    [Fact] public async Task SendThreadStateAsync_SendsCorrectJsonFormat()
    
    // Integration with FunctionDescriptionProvider
    [Fact] public async Task HandleToolCallStart_SendsStepStartedThenStatusUpdate()
    [Fact] public async Task HandleToolCallEnd_SendsStepFinished()
}
```

---

## Detailed Test Specifications

### 1. Domain Layer Tests (Unit Tests)

#### `TaskItem` Entity Tests

```csharp
// tests/TaskAgent.Domain.Tests/Entities/TaskItemTests.cs

public class TaskItemTests
{
    // Factory Method Tests
    [Fact] public void Create_WithValidData_ReturnsTaskItem()
    [Fact] public void Create_WithEmptyTitle_ThrowsArgumentException()
    [Fact] public void Create_WithTitleExceedingMaxLength_ThrowsArgumentException()
    [Fact] public void Create_SetsDefaultStatusToPending()
    [Fact] public void Create_SetsCreatedAtToUtcNow()
    
    // UpdateStatus Tests
    [Fact] public void UpdateStatus_FromPendingToInProgress_UpdatesStatus()
    [Fact] public void UpdateStatus_FromCompletedToPending_ThrowsInvalidOperationException()
    [Fact] public void UpdateStatus_SameStatus_DoesNothing()
    [Fact] public void UpdateStatus_UpdatesUpdatedAtTimestamp()
    
    // UpdatePriority Tests
    [Fact] public void UpdatePriority_ChangesToHigh_UpdatesPriority()
    [Fact] public void UpdatePriority_SamePriority_DoesNothing()
    
    // IsHighPriority Tests
    [Fact] public void IsHighPriority_WhenHigh_ReturnsTrue()
    [Fact] public void IsHighPriority_WhenMediumOrLow_ReturnsFalse()
}
```

**Expected: 14 tests**

---

### 2. Application Layer Tests (Unit Tests)

#### `TaskFunctions` Tests (Mocked Repository)

```csharp
// tests/TaskAgent.Application.Tests/Functions/TaskFunctionsTests.cs

public class CreateTaskTests
{
    // Happy Path
    [Fact] public async Task CreateTaskAsync_WithValidData_ReturnsSuccessMessage()
    [Fact] public async Task CreateTaskAsync_WithValidData_PersistsToRepository()
    
    // Validation
    [Fact] public async Task CreateTaskAsync_WithEmptyTitle_ReturnsErrorMessage()
    [Fact] public async Task CreateTaskAsync_WithInvalidPriority_ReturnsErrorMessage()
    [Fact] public async Task CreateTaskAsync_WithNullDescription_UsesEmptyString()
    
    // Priority Parsing
    [Theory]
    [InlineData("Low", TaskPriority.Low)]
    [InlineData("low", TaskPriority.Low)]
    [InlineData("HIGH", TaskPriority.High)]
    public async Task CreateTaskAsync_ParsesPriorityCaseInsensitively(string input, TaskPriority expected)
    
    // Telemetry
    [Fact] public async Task CreateTaskAsync_RecordsMetrics()
}

public class ListTasksTests
{
    [Fact] public async Task ListTasksAsync_WithNoFilters_ReturnsAllTasks()
    [Fact] public async Task ListTasksAsync_WithStatusFilter_ReturnsFilteredTasks()
    [Fact] public async Task ListTasksAsync_WithPriorityFilter_ReturnsFilteredTasks()
    [Fact] public async Task ListTasksAsync_WithBothFilters_ReturnsCombinedFilter()
    [Fact] public async Task ListTasksAsync_WithInvalidStatus_ReturnsErrorMessage()
    [Fact] public async Task ListTasksAsync_WhenNoTasksExist_ReturnsNoTasksMessage()
}

public class UpdateTaskTests
{
    [Fact] public async Task UpdateTaskAsync_WithValidId_UpdatesTask()
    [Fact] public async Task UpdateTaskAsync_WithInvalidId_ReturnsNotFoundMessage()
    [Fact] public async Task UpdateTaskAsync_WithInvalidStatus_ReturnsErrorMessage()
    [Fact] public async Task UpdateTaskAsync_StatusTransitionViolation_ReturnsErrorMessage()
}

public class DeleteTaskTests
{
    [Fact] public async Task DeleteTaskAsync_WithValidId_DeletesTask()
    [Fact] public async Task DeleteTaskAsync_WithInvalidId_ReturnsNotFoundMessage()
}

public class GetTaskDetailsTests
{
    [Fact] public async Task GetTaskDetailsAsync_WithValidId_ReturnsTaskDetails()
    [Fact] public async Task GetTaskDetailsAsync_WithInvalidId_ReturnsNotFoundMessage()
}

public class GetTaskSummaryTests
{
    [Fact] public async Task GetTaskSummaryAsync_ReturnsStatusCounts()
    [Fact] public async Task GetTaskSummaryAsync_ReturnsPriorityCounts()
    [Fact] public async Task GetTaskSummaryAsync_WhenEmpty_ReturnsZeroCounts()
}
```

**Expected: 28 tests**

---

### 3. Infrastructure Layer Tests (Testcontainers)

#### SQL Server Container Fixture

```csharp
// tests/TaskAgent.Infrastructure.Tests/Fixtures/SqlServerContainerFixture.cs

public class SqlServerContainerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container;
    public TaskDbContext DbContext { get; private set; }
    
    public SqlServerContainerFixture()
    {
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();
    }
    
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        
        var options = new DbContextOptionsBuilder<TaskDbContext>()
            .UseSqlServer(_container.GetConnectionString())
            .Options;
            
        DbContext = new TaskDbContext(options);
        await DbContext.Database.MigrateAsync();
    }
    
    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _container.DisposeAsync();
    }
}

[CollectionDefinition("SqlServer")]
public class SqlServerCollection : ICollectionFixture<SqlServerContainerFixture> { }
```

#### PostgreSQL Container Fixture

```csharp
// tests/TaskAgent.Infrastructure.Tests/Fixtures/PostgreSqlContainerFixture.cs

public class PostgreSqlContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    public ConversationDbContext DbContext { get; private set; }
    
    public PostgreSqlContainerFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("taskagent_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();
    }
    
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        
        var options = new DbContextOptionsBuilder<ConversationDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;
            
        DbContext = new ConversationDbContext(options);
        await DbContext.Database.MigrateAsync();
    }
    
    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _container.DisposeAsync();
    }
}

[CollectionDefinition("PostgreSql")]
public class PostgreSqlCollection : ICollectionFixture<PostgreSqlContainerFixture> { }
```

#### Repository Tests

```csharp
// tests/TaskAgent.Infrastructure.Tests/Repositories/TaskRepositoryTests.cs

[Collection("SqlServer")]
public class TaskRepositoryTests
{
    private readonly TaskRepository _repository;
    private readonly TaskDbContext _context;
    
    public TaskRepositoryTests(SqlServerContainerFixture fixture)
    {
        _context = fixture.DbContext;
        _repository = new TaskRepository(_context);
    }
    
    // CRUD Tests
    [Fact] public async Task AddAsync_InsertsTaskToDatabase()
    [Fact] public async Task GetByIdAsync_ReturnsTask_WhenExists()
    [Fact] public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    [Fact] public async Task GetAllAsync_ReturnsAllTasks_OrderedByCreatedAtDesc()
    [Fact] public async Task UpdateAsync_ModifiesTask()
    [Fact] public async Task DeleteAsync_RemovesTask()
    
    // Search Tests
    [Fact] public async Task SearchAsync_FiltersByStatus()
    [Fact] public async Task SearchAsync_FiltersByPriority()
    [Fact] public async Task SearchAsync_FiltersByBothStatusAndPriority()
    [Fact] public async Task SearchAsync_WithNoFilters_ReturnsAll()
}
```

**Expected: 10 tests**

#### Conversation Service Tests

```csharp
// tests/TaskAgent.Infrastructure.Tests/Services/ConversationServiceTests.cs

[Collection("PostgreSql")]
public class ConversationServiceTests
{
    private readonly ConversationService _service;
    private readonly ConversationDbContext _context;
    
    public ConversationServiceTests(PostgreSqlContainerFixture fixture)
    {
        _context = fixture.DbContext;
        _service = new ConversationService(_context, NullLogger<ConversationService>.Instance);
    }
    
    // ListThreads Tests
    [Fact] public async Task ListThreadsAsync_ReturnsPaginatedResults()
    [Fact] public async Task ListThreadsAsync_SortsByUpdatedAtDesc()
    [Fact] public async Task ListThreadsAsync_ExcludesInactiveThreads()
    
    // GetConversationHistory Tests
    [Fact] public async Task GetConversationHistoryAsync_ReturnsMessages()
    [Fact] public async Task GetConversationHistoryAsync_ReturnsNull_WhenNotFound()
    [Fact] public async Task GetConversationHistoryAsync_FiltersSystemMessages()
    
    // Delete Tests
    [Fact] public async Task DeleteThreadAsync_SetsIsActiveToFalse()
    [Fact] public async Task DeleteThreadAsync_ReturnsFalse_WhenNotFound()
}
```

**Expected: 8 tests**

---

### 4. WebApi Tests (API Integration)

#### Custom WebApplicationFactory

```csharp
// tests/TaskAgent.WebApi.Tests/Fixtures/CustomWebApplicationFactory.cs

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer;
    private readonly PostgreSqlContainer _postgresContainer;
    
    public CustomWebApplicationFactory()
    {
        _sqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();
            
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();
    }
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registrations
            var descriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<TaskDbContext>) ||
                            d.ServiceType == typeof(DbContextOptions<ConversationDbContext>))
                .ToList();
            foreach (var d in descriptors) services.Remove(d);
            
            // Add Testcontainers databases
            services.AddDbContext<TaskDbContext>(options =>
                options.UseSqlServer(_sqlContainer.GetConnectionString()));
                
            services.AddDbContext<ConversationDbContext>(options =>
                options.UseNpgsql(_postgresContainer.GetConnectionString()));
        });
    }
    
    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();
        await _postgresContainer.StartAsync();
    }
    
    public new async Task DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
        await _postgresContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
```

#### Controller Tests

```csharp
// tests/TaskAgent.WebApi.Tests/Controllers/ConversationsControllerTests.cs

public class ConversationsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    
    public ConversationsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }
    
    // GET /api/conversations
    [Fact] public async Task GetConversations_ReturnsOkWithPaginatedList()
    [Fact] public async Task GetConversations_WithPagination_RespectsPageSize()
    
    // GET /api/conversations/{threadId}/messages
    [Fact] public async Task GetMessages_WithValidThreadId_ReturnsMessages()
    [Fact] public async Task GetMessages_WithInvalidThreadId_ReturnsNotFound()
    
    // DELETE /api/conversations/{threadId}
    [Fact] public async Task DeleteConversation_WithValidId_ReturnsNoContent()
    [Fact] public async Task DeleteConversation_WithInvalidId_ReturnsNotFound()
}
```

**Expected: 6 tests**

---

## Test Data Builders & Fakers

### TaskItem Builder (Fluent API)

```csharp
// tests/TaskAgent.Tests.Common/Builders/TaskItemBuilder.cs

public class TaskItemBuilder
{
    private string _title = "Default Task";
    private string _description = "Default description";
    private TaskPriority _priority = TaskPriority.Medium;
    
    public TaskItemBuilder WithTitle(string title) { _title = title; return this; }
    public TaskItemBuilder WithDescription(string desc) { _description = desc; return this; }
    public TaskItemBuilder WithPriority(TaskPriority priority) { _priority = priority; return this; }
    public TaskItemBuilder AsHighPriority() { _priority = TaskPriority.High; return this; }
    
    public TaskItem Build() => TaskItem.Create(_title, _description, _priority);
}
```

### Bogus Faker

```csharp
// tests/TaskAgent.Tests.Common/Fakers/TaskItemFaker.cs

public class TaskItemFaker : Faker<TaskItemCreateDto>
{
    public TaskItemFaker()
    {
        RuleFor(t => t.Title, f => f.Lorem.Sentence(3, 5));
        RuleFor(t => t.Description, f => f.Lorem.Paragraph());
        RuleFor(t => t.Priority, f => f.PickRandom<TaskPriority>());
    }
}

// Usage in tests:
var faker = new TaskItemFaker();
var task = faker.Generate();
var tasks = faker.Generate(10);
```

---

## NuGet Package References

### Test Project Dependencies

```xml
<!-- tests/TaskAgent.Domain.Tests/TaskAgent.Domain.Tests.csproj -->
<ItemGroup>
  <PackageReference Include="xunit" Version="2.9.2" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  <PackageReference Include="FluentAssertions" Version="7.0.0" />
  <PackageReference Include="NSubstitute" Version="5.3.0" />
  <PackageReference Include="Bogus" Version="35.6.1" />
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
  <PackageReference Include="coverlet.collector" Version="6.0.2" />
</ItemGroup>

<!-- tests/TaskAgent.Infrastructure.Tests/TaskAgent.Infrastructure.Tests.csproj -->
<ItemGroup>
  <!-- All above plus: -->
  <PackageReference Include="Testcontainers.MsSql" Version="4.1.0" />
  <PackageReference Include="Testcontainers.PostgreSql" Version="4.1.0" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.0" />
</ItemGroup>

<!-- tests/TaskAgent.WebApi.Tests/TaskAgent.WebApi.Tests.csproj -->
<ItemGroup>
  <!-- All above plus: -->
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
</ItemGroup>
```

---

## CI/CD Considerations

### CI/CD Integration

For the complete GitHub Actions workflow configuration, see:
- **[.github/workflows/README.md](../../../../.github/workflows/README.md)** - Full CI/CD documentation
- **[.github/workflows/backend.yml](../../../../.github/workflows/backend.yml)** - Backend workflow file

**Key CI/CD Features:**
- ✅ Runs all 132 tests (Unit + Integration) on every push/PR
- ✅ Testcontainers works out-of-the-box on `ubuntu-latest` (Docker preinstalled)
- ✅ Combined code coverage report via ReportGenerator
- ✅ Test results and coverage artifacts uploaded for 30 days

### Docker Requirements for Testcontainers

- ✅ Docker must be running on CI agent
- ✅ GitHub Actions: Use `ubuntu-latest` (Docker preinstalled)
- ✅ Azure DevOps: Use Microsoft-hosted agents with Docker
- ⚠️ Windows runners do NOT support Testcontainers

---

## Test Summary

| Layer | Test Type | Planned | Implemented | Status |
|-------|-----------|---------|-------------|--------|
| Domain | Unit | 14 | 28 | ✅ Exceeded |
| Application | Unit | 28 | 75 | ✅ Exceeded |
| Infrastructure | Integration (Testcontainers) | 18 | 29 | ✅ Exceeded |
| WebApi | Functional (API) | 6 | 0 | 🚧 Planned |
| **Total** | | **66** | **132** | **✅ 200%** |

---

## Anti-Patterns to Avoid

❌ **Don't use InMemory database for EF Core tests** - It doesn't support SQL Server/PostgreSQL-specific features  
❌ **Don't share DbContext instances across tests** - Use fresh contexts per test for isolation  
❌ **Don't test private methods directly** - Test through public API  
❌ **Don't mock what you don't own** - Mock interfaces, not concrete classes  
❌ **Don't use Thread.Sleep in tests** - Use async patterns with CancellationToken  
❌ **Don't skip Testcontainers for repository tests** - Real database behavior differs from mocks

---

## Best Practices Checklist

- [x] **Use Arrange-Act-Assert pattern** - Clear test structure ✅
- [x] **One assertion per test** (when possible) - Clear failure messages ✅
- [x] **Use descriptive test names** - `MethodName_Scenario_ExpectedResult` ✅
- [x] **Use `[Theory]` for data-driven tests** - Reduce code duplication ✅
- [x] **Use Testcontainers for database tests** - Real database behavior ✅
- [x] **Use NSubstitute for mocking** - Clean syntax ✅
- [x] **Use FluentAssertions** - Readable assertions ✅
- [ ] **Use Bogus for fake data** - Realistic test data (planned)
- [x] **Run tests in parallel** - Faster CI/CD ✅
- [x] **Collect code coverage** - Combined report via ReportGenerator ✅

---

## Resources

- [Microsoft: Unit testing best practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
- [Microsoft: Integration tests in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [Testcontainers .NET Documentation](https://testcontainers.com/guides/getting-started-with-testcontainers-for-dotnet/)
- [xUnit Documentation](https://xunit.net/docs/getting-started/netcore/cmdline)
- [NSubstitute Documentation](https://nsubstitute.github.io/help/getting-started/)
- [FluentAssertions Documentation](https://fluentassertions.com/introduction)
- [Bogus Documentation](https://github.com/bchavez/Bogus)

---

**Last Updated**: December 7, 2025  
**Implemented Test Count**: 131 tests (198% of target)  
**Target Test Count**: 66+ tests
