# Dual Database Architecture Analysis

## Executive Summary

This document analyzes the **architectural implementation** of managing two separate databases in the TaskAgent application:

- **SQL Server**: TaskItem entities (CRUD operations)
- **PostgreSQL**: ConversationThreadEntity + ConversationMessage (chat persistence)

**Status**: ✅ **Working** - Both databases are properly configured with separate `DbContext` classes and connection strings.

---

## 1. Current Architecture (✅ Implemented)

### 1.1 Configuration (✅ Correct)

**File**: `appsettings.json` / `appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "TasksConnection": "Server=localhost;Database=TaskAgentDb;Trusted_Connection=true;Encrypt=False;",
    "ConversationsConnection": "Host=localhost;Port=5432;Database=taskagent_conversations;Username=postgres;Password=..."
  }
}
```

Both SQL Server (Tasks) and PostgreSQL (Conversations) are properly configured.

### 1.2 DbContext Configuration (✅ Correct)

**File**: `TaskAgent.Infrastructure/InfrastructureServiceExtensions.cs`

```csharp
public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
{
    // ✅ SQL Server for TaskItem entities
    string? tasksConnectionString = configuration.GetConnectionString("TasksConnection");
    services.AddDbContext<TaskDbContext>(options => options.UseSqlServer(tasksConnectionString));

    // ✅ PostgreSQL for Conversation entities  
    string? conversationsConnectionString = configuration.GetConnectionString("ConversationsConnection");
    services.AddDbContext<ConversationDbContext>(options => options.UseNpgsql(conversationsConnectionString));

    services.AddScoped<ITaskRepository, TaskRepository>();
    services.AddScoped<IAgentStreamingService, AgentStreamingService>();

    return services;
}
```

### 1.3 DbContext Entity Mapping (✅ Correct - Separate DbContexts)

**File**: `TaskAgent.Infrastructure/Data/TaskDbContext.cs` (SQL Server)

```csharp
public class TaskDbContext : DbContext
{
    public TaskDbContext(DbContextOptions<TaskDbContext> options) : base(options) { }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TaskItemConfiguration());
    }
}
```

**File**: `TaskAgent.Infrastructure/Data/ConversationDbContext.cs` (PostgreSQL)

```csharp
public class ConversationDbContext : DbContext
{
    public ConversationDbContext(DbContextOptions<ConversationDbContext> options) : base(options) { }

    public DbSet<ConversationThreadMetadata> ConversationThreads => Set<ConversationThreadMetadata>();
    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // PostgreSQL-specific types
        modelBuilder.Entity<ConversationMessage>()
            .Property(e => e.Timestamp).HasColumnType("timestamptz");
    }
}
```

**Solution Implemented**: Two separate `DbContext` classes, each connected to its own database.

---

## 2. Architecture Implementation

### Solution: Split DbContext (Implemented)

**Concept**: Two separate `DbContext` classes, one for each database.

#### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                   Application Layer                         │
│  ┌─────────────────────┐   ┌─────────────────────────────┐  │
│  │  ITaskRepository    │   │  IThreadPersistenceService  │  │
│  └─────────────────────┘   └─────────────────────────────┘  │
└────────────┬──────────────────────────────┬─────────────────┘
             │                              │
             ▼                              ▼
┌─────────────────────────┐   ┌─────────────────────────────┐
│   Infrastructure Layer  │   │   Infrastructure Layer      │
│  ┌──────────────────┐   │   │  ┌──────────────────────┐   │
│  │ TaskRepository   │   │   │  │ PostgresThread...    │   │
│  └────────┬─────────┘   │   │  └──────────┬───────────┘   │
│           │             │   │             │               │
│           ▼             │   │             ▼               │
│  ┌──────────────────┐   │   │  ┌──────────────────────┐   │
│  │ TaskDbContext    │   │   │  │ ConversationDbContext│   │
│  │ (SQL Server)     │   │   │  │ (PostgreSQL)         │   │
│  └────────┬─────────┘   │   │  └──────────┬───────────┘   │
└───────────┼─────────────┘   └─────────────┼───────────────┘
            │                               │
            ▼                               ▼
   ┌─────────────────┐           ┌─────────────────────┐
   │  SQL Server     │           │    PostgreSQL       │
   │  TaskAgentDb    │           │    TaskAgentDb      │
   └─────────────────┘           └─────────────────────┘
```

#### Implementation

**Step 1**: Split `TaskDbContext` into two contexts

**File**: `TaskAgent.Infrastructure/Data/TaskDbContext.cs`

```csharp
/// <summary>
/// DbContext for TaskItem entities stored in SQL Server.
/// </summary>
public class TaskDbContext : DbContext
{
    public TaskDbContext(DbContextOptions<TaskDbContext> options) : base(options) { }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply TaskItem configuration only
        modelBuilder.ApplyConfiguration(new TaskItemConfiguration());
    }
}
```

**File**: `TaskAgent.Infrastructure/Data/ConversationDbContext.cs` (NEW)

```csharp
/// <summary>
/// DbContext for ConversationThreadEntity stored in PostgreSQL.
/// </summary>
public class ConversationDbContext : DbContext
{
    public ConversationDbContext(DbContextOptions<ConversationDbContext> options) : base(options) { }

    public DbSet<ConversationThreadEntity> ConversationThreads => Set<ConversationThreadEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply ConversationThreadEntity configuration only
        modelBuilder.ApplyConfiguration(new ConversationThreadEntityConfiguration());
    }
}
```

**Step 2**: Update configuration files

**File**: `appsettings.json`

```json
{
  "ConnectionStrings": {
    "TasksConnection": "Server=localhost;Database=TaskAgentDb;Trusted_Connection=true;Encrypt=False;",
    "ConversationsConnection": "Host=localhost;Port=5432;Database=TaskAgentDb;Username=postgres;Password=postgres"
  },
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-key",
    "DeploymentName": "gpt-4o-mini"
  }
}
```

**Note**: Content safety is handled by Azure OpenAI's built-in content filtering. No separate configuration required.

**Step 3**: Register both DbContexts in DI

**File**: `TaskAgent.Infrastructure/InfrastructureServiceExtensions.cs`

```csharp
public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
{
    // ✅ Register SQL Server DbContext for Tasks
    string? tasksConnectionString = configuration.GetConnectionString("TasksConnection");
    services.AddDbContext<TaskDbContext>(options =>
    {
        options.UseSqlServer(tasksConnectionString);
    });

    // ✅ Register PostgreSQL DbContext for Conversations
    string? conversationsConnectionString = configuration.GetConnectionString("ConversationsConnection");
    services.AddDbContext<ConversationDbContext>(options =>
    {
        options.UseNpgsql(conversationsConnectionString);
    });

    // Repositories
    services.AddScoped<ITaskRepository, TaskRepository>();
    services.AddScoped<IThreadPersistenceService, PostgresThreadPersistenceService>();

    // Application Services
    services.AddScoped<IAgentStreamingService, AgentStreamingService>();
    services.AddScoped<IConversationService, ConversationService>();

    return services;
}
```

**Step 4**: Update repositories to use correct DbContext

**File**: `TaskAgent.Infrastructure/Repositories/TaskRepository.cs`

```csharp
public class TaskRepository : ITaskRepository
{
    private readonly TaskDbContext _context;  // ✅ Use TaskDbContext, not generic DbContext

    public TaskRepository(TaskDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<List<TaskItem>> GetAllAsync()
    {
        return await _context.Tasks
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    // ... other methods
}
```

**File**: `TaskAgent.Infrastructure/Services/PostgresThreadPersistenceService.cs`

```csharp
public class PostgresThreadPersistenceService : IThreadPersistenceService
{
    private readonly ConversationDbContext _context;  // ✅ Use ConversationDbContext

    public PostgresThreadPersistenceService(ConversationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task SaveThreadAsync(string threadId, string serializedThread)
    {
        var thread = await _context.ConversationThreads.FindAsync(threadId);

        if (thread == null)
        {
            thread = ConversationThreadEntity.Create(threadId, serializedThread);
            await _context.ConversationThreads.AddAsync(thread);
        }
        else
        {
            thread.Update(serializedThread, ExtractTitleFromThread(serializedThread), ExtractPreviewFromThread(serializedThread));
        }

        await _context.SaveChangesAsync();
    }

    // ... other methods
}
```

**Step 5**: Update migrations to be database-specific

```powershell
# Navigate to project directory
cd src/backend/services/TaskAgent/src

# Remove existing migration (contains mixed schemas)
dotnet ef migrations remove --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApp --context TaskDbContext

# Create SQL Server migration for Tasks
dotnet ef migrations add InitialTaskSchema --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApp --context TaskDbContext

# Create PostgreSQL migration for Conversations
dotnet ef migrations add InitialConversationSchema --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApp --context ConversationDbContext

# Apply migrations
dotnet ef database update --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApp --context TaskDbContext
dotnet ef database update --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApp --context ConversationDbContext
```

**Step 6**: Update automatic migration application in `Program.cs`

**File**: `TaskAgent.WebApp/Program.cs`

```csharp
// Apply migrations for BOTH databases on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        // Apply SQL Server migrations
        var taskContext = services.GetRequiredService<TaskDbContext>();
        await taskContext.Database.MigrateAsync();

        // Apply PostgreSQL migrations
        var conversationContext = services.GetRequiredService<ConversationDbContext>();
        await conversationContext.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating databases");
    }
}
```

---

### Solution 2: Schema-Based Separation (PostgreSQL Only)

**Concept**: Move **both** TaskItem and ConversationThreadEntity to PostgreSQL, but use different schemas.

#### Architecture Diagram

```
┌─────────────────────────────────────────────────────┐
│              Single DbContext                       │
│  ┌─────────────────┐   ┌────────────────────────┐   │
│  │  Tasks (schema) │   │  Conversations (schema)│   │
│  │  - TaskItem     │   │  - ConversationThread  │   │
│  └─────────────────┘   └────────────────────────┘   │
└────────────────────┬────────────────────────────────┘
                     │
                     ▼
            ┌─────────────────┐
            │   PostgreSQL    │
            │ localhost:5432  │
            │   TaskAgentDb   │
            └─────────────────┘
```

**Advantages**:

- ✅ Single database to manage
- ✅ Single connection string
- ✅ Easier deployment
- ✅ Can use transactions across both entity types

**Disadvantages**:

- ❌ Requires migrating existing Tasks data from SQL Server to PostgreSQL
- ❌ Changes original architecture decision (Tasks were intentionally in SQL Server)
- ❌ More complex migration path

#### Implementation

**Step 1**: Migrate TaskItem schema to PostgreSQL

**File**: `TaskAgent.Infrastructure/Data/TaskDbContext.cs`

```csharp
public class TaskDbContext : DbContext
{
    public TaskDbContext(DbContextOptions<TaskDbContext> options) : base(options) { }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<ConversationThreadEntity> ConversationThreads => Set<ConversationThreadEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ✅ TaskItem in "tasks" schema (PostgreSQL)
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.ToTable("Tasks", schema: "tasks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasColumnType("timestamptz");  // PostgreSQL type
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamptz");
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.Priority).HasConversion<int>();
        });

        // ✅ ConversationThreadEntity in "conversations" schema (PostgreSQL)
        modelBuilder.Entity<ConversationThreadEntity>(entity =>
        {
            entity.ToTable("ConversationThreads", schema: "conversations");
            entity.HasKey(e => e.ThreadId);
            entity.Property(e => e.SerializedThread).HasColumnType("json");  // json preserves property order
            entity.Property(e => e.CreatedAt).HasColumnType("timestamptz");
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamptz");
        });
    }
}
```

**Step 2**: Update connection string

**File**: `appsettings.json`

```json
{
  "ConnectionStrings": {
    "TasksConnection": "Server=localhost;Database=TaskAgentDb;Trusted_Connection=true;Encrypt=False;",
    "ConversationsConnection": "Host=localhost;Port=5432;Database=TaskAgentDb;Username=postgres;Password=postgres"
  }
}
```

**Step 3**: Migrate data from SQL Server to PostgreSQL

```powershell
# Export data from SQL Server
# (Manual process using SQL Server Management Studio or Azure Data Studio)

# Import into PostgreSQL
psql -U postgres -d TaskAgentDb -f tasks_export.sql
```

---

### Solution 3: Bounded Context Pattern (Domain-Driven Design)

**Concept**: Each aggregate root (Task, Conversation) has its own isolated persistence context.

**Architecture Diagram**:

```
┌──────────────────────────┐    ┌───────────────────────────┐
│   Task Bounded Context   │    │ Conversation Context      │
│  ┌────────────────────┐  │    │  ┌──────────────────────┐ │
│  │ TaskAggregate      │  │    │  │ ConversationAggregate│ │
│  │ - TaskItem         │  │    │  │ - ThreadEntity       │ │
│  │ - TaskRepository   │  │    │  │ - ThreadPersistence  │ │
│  │ - TaskDbContext    │  │    │  │ - ConversationDbCtx  │ │
│  └────────────────────┘  │    │  └──────────────────────┘ │
└──────────┬───────────────┘    └──────────┬────────────────┘
           │                               │
           ▼                               ▼
   ┌──────────────┐              ┌──────────────────┐
   │ SQL Server   │              │   PostgreSQL     │
   └──────────────┘              └──────────────────┘
```

**Key Principle**: Each bounded context is **completely isolated** with its own:

- Domain entities
- DbContext
- Migrations
- Database

**Advantages**:

- ✅ True separation of concerns (DDD principles)
- ✅ Each context can evolve independently
- ✅ Clear ownership and responsibilities
- ✅ Can use different databases optimized for each domain

**Disadvantages**:

- ❌ More complex folder structure
- ❌ Duplication of some infrastructure code
- ❌ Cannot use EF Core transactions across contexts (must use distributed transactions)

#### Implementation

**Folder Structure**:

```
TaskAgent.Infrastructure/
├── Tasks/                           # Task Bounded Context
│   ├── TaskDbContext.cs
│   ├── TaskRepository.cs
│   └── Migrations/
│       └── 20251114_InitialTaskSchema.cs
├── Conversations/                   # Conversation Bounded Context
│   ├── ConversationDbContext.cs
│   ├── PostgresThreadPersistenceService.cs
│   └── Migrations/
│       └── 20251114_InitialConversationSchema.cs
└── DependencyInjection.cs          # Registers both contexts
```

**Registration**:

```csharp
public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
{
    // Task Bounded Context
    services.AddDbContext<TaskDbContext>(options =>
        options.UseSqlServer(configuration.GetConnectionString("TasksConnection")));
    services.AddScoped<ITaskRepository, TaskRepository>();

    // Conversation Bounded Context
    services.AddDbContext<ConversationDbContext>(options =>
        options.UseNpgsql(configuration.GetConnectionString("ConversationsConnection")));
    services.AddScoped<IThreadPersistenceService, PostgresThreadPersistenceService>();

    return services;
}
```

---

## 3. Comparison Matrix

| Criteria                | Solution 1: Split DbContext | Solution 2: Schema-Based | Solution 3: Bounded Context      |
| ----------------------- | --------------------------- | ------------------------ | -------------------------------- |
| **Complexity**          | ⚠️ Medium                   | ✅ Low                   | ❌ High                          |
| **Migration Effort**    | ✅ Low (4-6 hours)          | ❌ High (data migration) | ⚠️ Medium (8-12 hours)           |
| **Database Management** | ⚠️ Two databases            | ✅ Single database       | ⚠️ Two databases                 |
| **Deployment**          | ⚠️ Two connection strings   | ✅ One connection string | ⚠️ Two connection strings        |
| **Transaction Support** | ❌ No cross-DB transactions | ✅ Full ACID across both | ❌ Distributed transactions only |
| **Clean Architecture**  | ✅ Good separation          | ⚠️ Shared DbContext      | ✅ Excellent (DDD)               |
| **Performance**         | ✅ Optimized per DB         | ✅ Same                  | ✅ Optimized per DB              |
| **Scalability**         | ✅ Independent scaling      | ⚠️ Single DB bottleneck  | ✅ Independent scaling           |
| **Testing**             | ✅ Easy to mock             | ✅ Easy to mock          | ✅ Easy to mock                  |
| **Future Flexibility**  | ✅ Can swap DB per context  | ❌ Locked to PostgreSQL  | ✅ Full flexibility              |

---

## 4. Recommendation

### For TaskAgent MVP (Current Stage)

**Recommended Solution**: **Solution 1 (Split DbContext)**

**Rationale**:

1. ✅ **Preserves original design**: Tasks in SQL Server, Conversations in PostgreSQL
2. ✅ **Low migration effort**: No data migration needed, only code refactoring
3. ✅ **Clean separation**: Each entity type has dedicated DbContext
4. ✅ **Aligns with project instructions**: `.github/copilot-instructions.md` mentions "SQL Server (via connection string 'Server=localhost;Database=TaskAgentDb;Trusted_Connection=true;Encrypt=False;')"
5. ✅ **Future-proof**: Can easily optimize each database independently

### Implementation Plan (Solution 1)

**Estimated Time**: 4-6 hours

#### Phase 1: Create Separate DbContexts (1 hour)

1. Create `TaskAgent.Infrastructure/Data/ConversationDbContext.cs`
2. Split `TaskDbContext.cs` to contain only `Tasks` DbSet
3. Update entity configurations to use appropriate DbContext

#### Phase 2: Update DI Registration (30 minutes)

1. Update `appsettings.json` with two connection strings
2. Modify `InfrastructureServiceExtensions.cs` to register both DbContexts
3. Update `Program.cs` to apply both migrations on startup

#### Phase 3: Update Repositories (1 hour)

1. Update `TaskRepository` constructor to inject `TaskDbContext`
2. Update `PostgresThreadPersistenceService` constructor to inject `ConversationDbContext`
3. Verify no shared DbContext dependencies

#### Phase 4: Recreate Migrations (1 hour)

1. Remove existing mixed migration
2. Create SQL Server migration for Tasks
3. Create PostgreSQL migration for Conversations
4. Test migration application on clean databases

#### Phase 5: Testing & Validation (2 hours)

1. Unit tests for both repositories
2. Integration tests with both databases
3. Verify automatic migration on startup
4. Load testing with concurrent requests to both databases

---

## 5. Implementation Commands

### Step-by-Step Execution (Solution 1)

```powershell
# Navigate to working directory
cd TaskAgent-AgenticAI\src\backend\services\TaskAgent\src

# Step 1: Remove existing mixed migration
dotnet ef migrations remove --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApp

# Step 2: Drop existing databases (fresh start)
dotnet ef database drop --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApp --force

# Step 3: Create TaskDbContext migration (SQL Server)
dotnet ef migrations add InitialTaskSchema --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApp --context TaskDbContext --output-dir Migrations/TaskDb

# Step 4: Create ConversationDbContext migration (PostgreSQL)
dotnet ef migrations add InitialConversationSchema --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApp --context ConversationDbContext --output-dir Migrations/ConversationDb

# Step 5: Apply migrations
dotnet ef database update --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApp --context TaskDbContext
dotnet ef database update --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApp --context ConversationDbContext

# Step 6: Verify databases
# SQL Server: Connect to localhost and check TaskAgentDb.Tasks table
# PostgreSQL: psql -U postgres -d TaskAgentDb -c "\dt"

# Step 7: Build and run
cd ../../../../../../
dotnet run --project src/TaskAgent.AppHost
```

---

## 6. Testing Checklist

After implementing Solution 1, verify:

- [ ] **SQL Server Connection**: TaskRepository can create/read/update/delete Tasks
- [ ] **PostgreSQL Connection**: PostgresThreadPersistenceService can save/load threads
- [ ] **Migrations Applied**: Both databases have correct schemas
- [ ] **Startup Migrations**: `dotnet run` applies pending migrations automatically
- [ ] **Connection String Validation**: Application throws clear error if either connection string is missing
- [ ] **Concurrent Operations**: Can create Task and save Conversation simultaneously without conflicts
- [ ] **Transaction Isolation**: Failure in one database doesn't affect the other
- [ ] **AppHost Integration**: .NET Aspire orchestrates both database connections

---

## 7. Alternative Considerations

### When to Choose Solution 2 (Schema-Based)

**Choose this if**:

- You want to simplify deployment (single database)
- You need transactions across Tasks and Conversations
- You prefer PostgreSQL for all data (better JSON support, free/open-source)
- Your hosting environment charges per database (Azure SQL Database)

**Migration Path**:

1. Export SQL Server Tasks data to CSV/JSON
2. Import into PostgreSQL `tasks` schema
3. Update connection strings
4. Simplify DbContext to single instance

### When to Choose Solution 3 (Bounded Context)

**Choose this if**:

- You're following strict DDD principles
- You plan to extract contexts into separate microservices later
- You have separate teams owning Task and Conversation domains
- You want maximum decoupling between domains

---

## 8. Potential Issues & Mitigations

### Issue 1: Cross-Database Transactions

**Scenario**: User creates Task and starts Conversation in same HTTP request. If Conversation save fails, should Task be rolled back?

**Solution**: Use **Saga Pattern** or **eventual consistency**:

```csharp
public async Task<CreateTaskResult> CreateTaskWithConversationAsync(CreateTaskRequest request)
{
    TaskItem? createdTask = null;
    string? threadId = null;

    try
    {
        // Step 1: Create task in SQL Server
        createdTask = await _taskRepository.AddAsync(request.ToTaskItem());

        // Step 2: Create conversation in PostgreSQL
        threadId = await _threadPersistence.CreateThreadAsync(request.InitialMessage);

        return new CreateTaskResult { Task = createdTask, ThreadId = threadId };
    }
    catch (Exception ex)
    {
        // Compensating transaction: rollback task creation
        if (createdTask != null)
        {
            await _taskRepository.DeleteAsync(createdTask.Id);
        }

        throw;
    }
}
```

### Issue 2: Migration Ordering

**Scenario**: TaskDbContext migration references ConversationDbContext entity (foreign key-like relationship).

**Solution**: Keep domains **completely isolated**. Use **application-level references** (store ThreadId as string in TaskItem, not foreign key).

```csharp
public class TaskItem
{
    public int Id { get; private set; }
    public string Title { get; private set; }

    // ✅ Application-level reference (not DB foreign key)
    public string? ConversationThreadId { get; private set; }
}
```

---

## 9. Documentation Updates

After implementing Solution 1, update these documents:

1. **README.md**: Add both connection strings to Prerequisites section
2. **POSTGRESQL_MIGRATION.md**: Clarify that PostgreSQL is ONLY for Conversations
3. **`.github/copilot-instructions.md`**: Update architecture section with dual-database diagram
4. **`src/ASPIRE_VERSION.md`**: Add note about database orchestration in Aspire

---

## 10. Conclusion

### Immediate Action Required

**Problem**: Application currently has **broken database configuration** (single PostgreSQL connection, but TaskItem expects SQL Server).

**Solution**: Implement **Solution 1 (Split DbContext)** with two connection strings:

- `TasksConnection`: SQL Server for Tasks
- `ConversationsConnection`: PostgreSQL for Conversations

**Timeline**: 4-6 hours to complete implementation and testing.

**Risk**: Low (no production data exists yet).

### Next Steps

1. ✅ Review this analysis document
2. ✅ Approve Solution 1 (or choose alternative)
3. 🔧 Execute implementation plan (Phase 1-5)
4. ✅ Verify testing checklist
5. 📚 Update project documentation
6. 🚀 Deploy to development environment

---

## 11. References

- [EF Core Multiple DbContext - Microsoft Docs](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/)
- [PostgreSQL Schema Documentation](https://www.postgresql.org/docs/current/ddl-schemas.html)
- [.NET Aspire Multi-Database Support](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling)
- [Domain-Driven Design Bounded Contexts - Martin Fowler](https://martinfowler.com/bliki/BoundedContext.html)
