# Task Agent - Copilot Instructions

## Architecture

**Clean Architecture** with strict downward dependencies: `Domain` → `Application` → `Infrastructure` → `WebApp`

**Critical**: Domain has NO external dependencies. Application defines interfaces (e.g., `ITaskRepository`); Infrastructure implements them (`TaskRepository`).

### Layer Responsibilities

```
Domain/          - Entities (TaskItem), Enums, Constants, NO external dependencies
Application/     - DTOs, Interfaces (ITaskRepository, ITaskAgentService), Functions (TaskFunctions)
Infrastructure/  - EF Core (TaskDbContext), Repositories, Azure Services (ContentSafetyService)
WebApp/          - Controllers, Middleware, DI setup, Agent factory (TaskAgentService)
```

**Dependency Flow**: Each layer only references the one below. Infrastructure and WebApp both depend on Application, but NOT on each other.

## AI Agent Pattern

### Microsoft Agentic AI Framework

This project uses `Microsoft.Agents.AI.OpenAI` (preview) - Microsoft's framework for building autonomous AI agents with function calling.

**Key APIs**:

```csharp
// 1. Get chat client from Azure OpenAI
ChatClient chatClient = azureClient.GetChatClient(deploymentName);

// 2. Create agent with tools and instructions
AIAgent agent = chatClient.CreateAIAgent(
    instructions: "System prompt with behavioral rules...",
    tools: [createTaskTool, listTasksTool, ...]
);

// 3. Create conversation thread
AgentThread thread = await agent.CreateThreadAsync();

// 4. Add user message
await thread.AddUserMessageAsync("Create a high priority task");

// 5. Invoke agent (streams responses)
await foreach (StreamingResponse response in thread.InvokeAsync(agent)) {
    if (response is StreamingTextResponse textResponse) {
        // Process streamed text
    }
}

// 6. Serialize thread for persistence
string serialized = thread.Serialize();
```

**Thread lifecycle**: Each HTTP request loads thread, processes message, saves updated thread.

### Factory Method (`TaskAgentService.CreateAgent`)

```csharp
// Wires: AzureOpenAIClient + TaskFunctions (6 tools) + 170-line instruction prompt
var agent = chatClient.CreateAIAgent(instructions: "...", tools: [...]);
```

**Key Points**:

- Agent is **scoped per request** (registered in `DependencyInjection.AddPresentation()`) - never singleton
- Each conversation gets a **thread ID** for context persistence via thread serialization
- Instructions embed behavioral rules: immediate task creation, Markdown tables with emojis, contextual suggestions
- Tools created with `AIFunctionFactory.Create(taskFunctions.MethodName)`
- Agent factory is a static method that takes `AzureOpenAIClient`, `modelDeployment`, and `ITaskRepository`

### Function Tools Contract

All 6 methods in `TaskFunctions.cs` **must**:

- Decorate with `[Description]` on method AND parameters for AI understanding
- Return user-friendly strings (✅/❌ emojis) - **NEVER throw exceptions to AI**
- Catch all exceptions internally, return formatted error strings (see `Application/Constants/ErrorMessages.cs`)
- Use emoji patterns: 🔴 High, 🟡 Medium, 🟢 Low; ⏳ Pending, 🔄 InProgress, ✅ Completed
- Methods: `CreateTask`, `ListTasks`, `GetTaskDetails`, `UpdateTask`, `DeleteTask`, `GetTaskSummary`

**Adding New Function Tools**:

1. Add method to `TaskFunctions.cs` with `[Description]` attributes
2. Register in `TaskAgentService.CreateAgent()`: `AIFunctionFactory.Create(taskFunctions.NewMethod)`
3. Update 170-line instruction string if behavioral guidance needed
4. Test that tool never throws exceptions to AI

## Domain Patterns

### Entity Creation - Factory Method Required

```csharp
// ❌ NEVER: new TaskItem() { Title = "..." }
// ✅ ALWAYS: TaskItem.Create(title, description, priority)
```

- Private setters + factory method enforce validation (title ≤ 200 chars, see `Domain/Constants/TaskConstants.cs`)
- Private parameterless constructor for EF Core only
- Business rules in entity: `UpdateStatus()` blocks Completed → Pending transitions
- Validation throws `ArgumentException` with messages from `Domain/Constants/ValidationMessages.cs`

### Entity Updates

```csharp
// ✅ Use entity methods for state changes
task.UpdateStatus(TaskStatus.InProgress);  // Validates business rules
task.UpdatePriority(TaskPriority.High);

// ❌ NEVER: task.Status = TaskStatus.InProgress (won't compile - private setter)
```

## Critical Workflows

### Working Directory & Project Structure

**Repository structure**:

```
src/
├── TaskAgent.AppHost/                  # Aspire orchestrator (root level)
├── backend/
│   ├── TaskAgent.ServiceDefaults/      # Shared telemetry config
│   ├── TaskAgentWeb.sln                # Backend solution file
│   └── services/TaskAgent/src/         # Clean Architecture projects
│       ├── TaskAgent.Domain/
│       ├── TaskAgent.Application/
│       ├── TaskAgent.Infrastructure/
│       └── TaskAgent.WebApp/
└── frontend/                           # Next.js frontend (future)
```

**Working directory for EF commands**: `src/backend/services/TaskAgent/src/`

### EF Migrations (Multi-Project Solution)

```powershell
# Navigate to backend service directory first
cd src/backend/services/TaskAgent/src

# Always specify BOTH projects (Infrastructure has DbContext, WebApp has startup/DI):
dotnet ef migrations add MigrationName --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApp
dotnet ef database update --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApp

# View migrations
dotnet ef migrations list --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApp

# Drop database (for reset)
dotnet ef database drop --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApp
```

**Why Both?**: Infrastructure contains `TaskDbContext`, but WebApp has configuration (`appsettings.json` connection string) and DI setup.

**Automatic Migrations on Startup**:

The application **automatically applies pending migrations** on startup in ALL environments via:

```csharp
// Program.cs
await app.ApplyDatabaseMigrationsAsync();
```

This calls `DatabaseMigrationService.ApplyDatabaseMigrationsAsync()` in the Infrastructure layer, which creates/updates the database without manual intervention. No need to run `dotnet ef database update` manually unless troubleshooting.

### Running the Application

**With .NET Aspire (Recommended)**:

```powershell
# From repository root - includes Aspire Dashboard at https://localhost:17198
dotnet run --project src/TaskAgent.AppHost

# Provides:
# - Application at https://localhost:5001
# - Aspire Dashboard at https://localhost:17198 (OTLP telemetry visualization)
# - Automatic service orchestration
```

**Standalone (Without Aspire)**:

```powershell
# From repository root
dotnet run --project src/backend/services/TaskAgent/src/TaskAgent.WebApp

# Or from src/backend/services/TaskAgent/src/
dotnet run --project TaskAgent.WebApp
```

**Required Configuration** (in `appsettings.Development.json`):

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-key",
    "DeploymentName": "gpt-4o-mini"
  },
  "ContentSafety": {
    "Endpoint": "https://your-resource.cognitiveservices.azure.com/",
    "ApiKey": "your-key",
    "HateThreshold": 2,
    "ViolenceThreshold": 2,
    "SexualThreshold": 2,
    "SelfHarmThreshold": 2
  }
}
```

**Note**: ContentSafety config is required (throws `InvalidOperationException` if missing). App validates on startup via `app.ValidateConfiguration()`.

## Content Safety Middleware

**2-Layer Parallel Defense** on `/api/chat` POST endpoints:

1. **Prompt Shield** (Prompt Injection Detection) → REST API call to `/contentsafety/text:shieldPrompt`
2. **Content Moderation** (Harmful Content) → SDK call to `ContentSafetyClient.AnalyzeTextAsync()`

**Architecture**:

- Middleware: `WebApp/Middleware/ContentSafetyMiddleware.cs`
- Service: `Infrastructure/Services/ContentSafetyService.cs`
- Extension: `app.UseContentSafety()` in `Program.cs` (before `UseAuthorization()`)
- HttpClient: Named client via `IHttpClientFactory` (registered in `Infrastructure/DependencyInjection.cs`)

**Performance Optimization**: Both layers execute in parallel using `Task.WhenAll` for ~50% faster response (~200-400ms vs ~400-800ms sequential). Security priority: checks injection result first, then content result.

**Blocking Behavior**:

- Prompt injection detected → 400 Bad Request with `PromptInjectionResult`
- Content policy violation → 400 Bad Request with `ContentSafetyResult` and violated categories

## Project-Specific Conventions

### Naming Quirk - TaskStatus Collision

`TaskStatus` enum collision with `System.Threading.Tasks.TaskStatus` resolved with alias:

```csharp
using DomainTaskStatus = TaskAgent.Domain.Enums.TaskStatus;
```

Use `DomainTaskStatus` in files that also use `Task<T>` (common in async code).

### Error Handling Strategy by Layer

- **Domain**: Throw `ArgumentException` for validation failures (title empty, too long, invalid state transitions)
- **Function Tools**: NEVER throw - catch & return user-friendly error strings with emojis
- **Controllers**: `BadRequest(400)` for validation, `StatusCode(500)` for unexpected errors
- **Middleware**: Log errors, fail open (allow request to proceed) for availability

### Constants Pattern

All magic strings/numbers in constant files:

- `Domain/Constants/TaskConstants.cs`: MAX_TITLE_LENGTH (200)
- `Domain/Constants/ValidationMessages.cs`: Validation error messages
- `Application/Constants/ErrorMessages.cs`: Function tool error messages
- `Infrastructure/Constants/ContentSafetyConstants.cs`: API paths, HTTP client name
- `WebApp/Constants/ApiRoutes.cs`: Route constants (CHAT = "api/chat")
- `WebApp/Constants/ErrorCodes.cs`: Error codes for API responses

### Dependency Injection Pattern

Each layer has a service extension class with extension methods:

```csharp
// Application/ApplicationServiceExtensions.cs
services.AddApplication();                  // Registers telemetry (AgentMetrics singleton)

// Infrastructure/InfrastructureServiceExtensions.cs
services.AddInfrastructure(configuration);  // Registers DbContext, Repositories, ContentSafetyClient

// WebApp/PresentationServiceExtensions.cs
services.AddPresentation(configuration);    // Registers Controllers, AIAgent (scoped)
```

Called in `Program.cs`:

```csharp
builder.AddServiceDefaults();  // Aspire: OpenTelemetry, health checks, resilience

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddPresentation(builder.Configuration);
```

**Layer Registration Order**: Application → Infrastructure → Presentation (following dependency direction).

## Integration Points

### EF Core Patterns

- **Read-only queries**: Always use `.AsNoTracking()` for better performance (see `TaskRepository.GetAllAsync()`)
- **Tracked updates**: Don't use `.AsNoTracking()` when updating entities
- **Enums**: Stored as ints in SQL Server with `.HasConversion<int>()` in entity configuration
- **Indexes**: On Status, Priority, CreatedAt for filtering performance
- **Connection**: LocalDB via connection string `"Server=(localdb)\\mssqllocaldb;Database=TaskAgentDb;Trusted_Connection=true;"`

### Azure OpenAI Client Setup

```csharp
// Registered in WebApp/PresentationServiceExtensions.cs
var client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
var chatClient = client.GetChatClient(modelDeployment);
var agent = chatClient.CreateAIAgent(instructions: "...", tools: [...]);
```

**Scoped Lifetime**: Agent must be scoped per request (not singleton) to maintain separate conversation contexts.

### Content Safety HttpClient

```csharp
// Infrastructure/InfrastructureServiceExtensions.cs
services.AddHttpClient("ContentSafety", client => {
    client.BaseAddress = new Uri(endpoint);
    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
});
```

Uses `IHttpClientFactory` for connection pooling and proper DNS refresh handling.

## Common Modifications

### Adding a New Entity

1. Create entity in `Domain/Entities/` with factory method and private setters
2. Add `DbSet<NewEntity>` to `TaskDbContext.cs`
3. Create entity configuration class in `Infrastructure/Data/Configurations/`
4. Run EF migration (see Critical Workflows)
5. Create repository interface in `Application/Interfaces/`
6. Implement repository in `Infrastructure/Repositories/`
7. Register in `Infrastructure/InfrastructureServiceExtensions.cs`

### Modifying Agent Behavior

Edit the 170-line instruction string in `TaskAgentService.CreateAgent()` - this is the single source of truth for agent personality and behavior rules.

**Important sections**:

- Task creation behavior (immediate vs. asking for confirmation)
- Presentation format guidelines (Markdown tables, emojis)
- Response style (professional, efficient, contextual suggestions)

### Extending Content Safety

To add a new safety layer:

1. Add method to `IContentSafetyService` interface
2. Implement in `Infrastructure/Services/ContentSafetyService.cs`
3. Call in `ContentSafetyMiddleware.InvokeAsync()` (add to `Task.WhenAll` for parallel execution)
4. Update blocking logic in middleware

## Anti-Patterns

❌ `new TaskItem()` → Use `TaskItem.Create()` factory  
❌ Singleton agent → Must be scoped per request  
❌ Throw from function tools → Return user-friendly error strings  
❌ Skip `.AsNoTracking()` on read queries → Performance degradation  
❌ Multiple instruction sources → Single string in `CreateAgent()`  
❌ Magic strings/numbers → Use constants from appropriate layer  
❌ Domain layer dependencies → Keep it dependency-free  
❌ Sequential safety checks → Use `Task.WhenAll` for parallel execution

## Key Files Reference

| File                                                          | Purpose                                               |
| ------------------------------------------------------------- | ----------------------------------------------------- |
| `Program.cs`                                                  | Application entry, middleware pipeline, DI bootstrap  |
| `WebApp/PresentationServiceExtensions.cs`                     | WebApp layer services, AIAgent factory registration   |
| `WebApp/Services/TaskAgentService.cs`                         | Agent factory method, 170-line instructions, chat API |
| `Application/Functions/TaskFunctions.cs`                      | 6 function tools for AI agent                         |
| `Domain/Entities/TaskItem.cs`                                 | Core entity, factory method, business rules           |
| `Infrastructure/Services/ContentSafetyService.cs`             | 2-layer content safety (Prompt Shield + Moderation)   |
| `WebApp/Middleware/ContentSafetyMiddleware.cs`                | Middleware applying safety checks to `/api/chat`      |
| `Infrastructure/Data/TaskDbContext.cs`                        | EF Core DbContext, entity configurations              |
| `Infrastructure/Repositories/TaskRepository.cs`               | Repository pattern implementation                     |
| `Infrastructure/Services/InMemoryThreadPersistenceService.cs` | Thread state persistence (in-memory, singleton)       |

## Package Management - Central Version Control

**CRITICAL**: This project uses **Central Package Management** (CPM) via `Directory.Packages.props`.

**How it works**:

- `Directory.Build.props` → Common project settings (target framework, nullable, code analysis, **`$(AspireVersion)` property**)
- `global.json` → MSBuild SDK versions (Aspire.AppHost.Sdk version **must match `$(AspireVersion)`**)
- `Directory.Packages.props` → Centralized NuGet package versions (uses `$(AspireVersion)` variable)
- Individual `.csproj` files → Package references WITHOUT version numbers

**Adding a new package**:

```powershell
# 1. Add to Directory.Packages.props
<PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />

# 2. Reference in .csproj (NO version attribute)
<PackageReference Include="Newtonsoft.Json" />
```

**Updating Aspire version** (see `ASPIRE_VERSION.md`):

```powershell
# 1. Update Directory.Build.props
<AspireVersion>9.5.2</AspireVersion>

# 2. Update global.json (must match!)
"Aspire.AppHost.Sdk": "9.5.2"

# 3. Restore
dotnet restore
```

**Key packages**:

- `Microsoft.Agents.AI.OpenAI` (1.0.0-preview.251107.1) - Agentic AI Framework
- `Azure.AI.OpenAI` (2.1.0) - Azure OpenAI SDK
- `Azure.AI.ContentSafety` (1.0.0) - Content Safety SDK
- `Microsoft.EntityFrameworkCore.SqlServer` (9.0.10)
- `Aspire.Hosting.AppHost` (via `$(AspireVersion)`) - .NET Aspire orchestration
- `SonarAnalyzer.CSharp` (10.15.0) - Code quality analysis

**Why CPM?**: Ensures version consistency across all projects, prevents dependency conflicts, simplifies upgrades.

## .NET Aspire & Observability Architecture

**Three-Project Aspire Structure**:

1. **`TaskAgent.AppHost`** - Orchestration layer (runs via `dotnet run --project src/TaskAgent.AppHost`)
   - Entry point: `AppHost.cs` with `builder.AddProject<Projects.TaskAgent_WebApp>("taskagent-webapp")`
   - Provides Aspire Dashboard at https://localhost:17198
   - Manages service lifecycle and discovery
2. **`TaskAgent.ServiceDefaults`** - Shared telemetry configuration (inside `src/backend/`)
   - `ServiceDefaultsExtensions.AddServiceDefaults()` registers OpenTelemetry, health checks, resilience
   - **Hybrid telemetry**: Auto-detects exporter based on environment
     - Development: `OTEL_EXPORTER_OTLP_ENDPOINT` → OTLP → Aspire Dashboard
     - Production: `APPLICATIONINSIGHTS_CONNECTION_STRING` → Azure Monitor → Application Insights
   - Registered custom sources: `"TaskAgent.Agent"`, `"TaskAgent.Functions"`
   - Security: HTTPS-only service discovery in production
3. **`TaskAgent.WebApp`** - Application (calls `builder.AddServiceDefaults()` in `Program.cs`)

**Custom Telemetry Components** (Application layer):

- `AgentMetrics` (singleton) - Custom OpenTelemetry meter (`"TaskAgent.Agent"`)
  - Counters: `agent.requests`, `agent.function_calls`, `agent.errors`
  - Histogram: `agent.response.duration` (ms)
- `AgentActivitySource` (static) - Custom activity source for distributed tracing
  - Span: `Agent.ProcessMessage` - end-to-end message processing
  - Span: `Function.{FunctionName}` - individual function tool calls
  - Tags: `thread.id`, `function.name`, `message.length`, `response.length`

**Usage in code**:

```csharp
// Metrics - injected via DI (singleton)
_metrics.RecordRequest(threadId, "success");
_metrics.RecordFunctionCall("CreateTask", "success");
_metrics.RecordResponseDuration(durationMs, threadId, success: true);

// Tracing - static utility
using Activity? activity = AgentActivitySource.StartMessageActivity(threadId, message);
using Activity? funcActivity = AgentActivitySource.StartFunctionActivity("CreateTask");
```

**Important**: EF Core SQL command logging is **disabled in production** via environment check in `ServiceDefaultsExtensions.cs` to prevent sensitive data leakage.

## Thread Persistence & Conversation State

**Architecture**: Conversations are persisted using `IThreadPersistenceService` to maintain context across HTTP requests.

**Flow**:

1. User sends message with optional `threadId`
2. If `threadId` exists → deserialize thread state: `AgentThread.Deserialize(serializedState)`
3. Agent processes message with full conversation history
4. After response → serialize thread state: `thread.Serialize()`
5. Save to persistence layer: `await _threadPersistence.SaveThreadAsync(threadId, serializedState)`

**Implementations**:

- **Development**: `InMemoryThreadPersistenceService` (ConcurrentDictionary, singleton)
- **Production**: Implement with Redis/SQL Server (scoped/transient)

**Critical**: Thread state contains entire conversation history - must be serialized/deserialized for each request.

**Code example** (see `TaskAgentService.SendMessageAsync`):

```csharp
// Load existing thread or create new
string? serializedThread = await _threadPersistence.GetThreadAsync(threadId);
AgentThread thread = string.IsNullOrEmpty(serializedThread)
    ? await _agent.CreateThreadAsync()
    : AgentThread.Deserialize(serializedThread);

// Process message
await thread.AddUserMessageAsync(message);
await foreach (var response in thread.InvokeAsync(_agent)) { }

// Save updated thread
await _threadPersistence.SaveThreadAsync(threadId, thread.Serialize());
```

**Production considerations**:

- Thread state grows with conversation length
- Consider TTL/cleanup strategies for old threads
- Scoped lifetime for multi-server scenarios

## Testing Guidance

**Content Safety**: See `CONTENT_SAFETY.md` for 75+ test cases including prompt injections, harmful content, edge cases, and troubleshooting.

**AI Agent**: Test via web UI at `http://localhost:5000` or POST to `/api/chat` endpoint with `{"message": "your message", "threadId": "optional-thread-id"}`

**Database**: Auto-created on first run. To reset: `dotnet ef database drop --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApp`

**Key Test Scenarios**:

1. Task creation with validation (title length, priority values)
2. Business rule enforcement (can't reopen completed tasks)
3. Thread persistence across multiple requests (same threadId)
4. Content safety blocking (prompt injection, harmful content)
5. Parallel safety checks performance (~200-400ms)
