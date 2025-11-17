# Task Agent - Copilot Instructions

## Project Overview

AI-powered task management system built with **Microsoft Agent Framework** (preview), demonstrating autonomous AI agents with function calling, Clean Architecture, and production-grade observability.

**Tech Stack**: .NET 10, ASP.NET Core MVC, Azure OpenAI (GPT-4o-mini), Azure AI Content Safety, Entity Framework Core, .NET Aspire 13.0.0, OpenTelemetry

## Architecture

**Clean Architecture** with strict downward dependencies: `Domain` → `Application` → `Infrastructure` → `WebApi`

**Critical**: Domain has NO external dependencies. Application defines interfaces (e.g., `ITaskRepository`); Infrastructure implements them (`TaskRepository`).

### Layer Responsibilities

```
Domain/          - Entities (TaskItem), Enums, Constants, NO external dependencies
Application/     - DTOs, Interfaces (ITaskRepository, ITaskAgentService), Functions (TaskFunctions)
Infrastructure/  - EF Core (TaskDbContext), Repositories, Azure Services (ContentSafetyService)
WebApi/          - Controllers, Middleware, DI setup, Agent factory (TaskAgentService)
```

**Dependency Flow**: Each layer references only the one below. Infrastructure and WebApi both depend on Application, but NOT on each other.

**Service Extension Pattern**: Each layer has `{Layer}ServiceExtensions.cs` with extension methods:

- `ApplicationServiceExtensions.AddApplication()` - Registers telemetry (AgentMetrics singleton)
- `InfrastructureServiceExtensions.AddInfrastructure(configuration)` - DbContext, Repositories, ContentSafetyClient
- `PresentationServiceExtensions.AddPresentation(configuration)` - Controllers, AIAgent (scoped)

## AI Agent Pattern

### Microsoft Agent Framework

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
│       └── TaskAgent.WebApi/
└── frontend/                           # Next.js frontend
```

**Working directory for EF commands**: `src/backend/services/TaskAgent/src/`

### EF Migrations (Multi-Project, Multi-Database Solution)

**Two separate DbContexts with different connection strings**:

1. **SQL Server** (`TaskDbContext`) - Task management entities
2. **PostgreSQL** (`ConversationDbContext`) - Conversation threads (JSON storage)

```powershell
# Navigate to backend service directory first
cd src/backend/services/TaskAgent/src

# SQL Server migrations (Tasks)
dotnet ef migrations add MigrationName --context TaskDbContext --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApi --output-dir Migrations/TaskDb

# PostgreSQL migrations (Conversations)
dotnet ef migrations add MigrationName --context ConversationDbContext --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApi --output-dir Migrations/ConversationDb

# Apply migrations (specify context)
dotnet ef database update --context TaskDbContext --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApi
dotnet ef database update --context ConversationDbContext --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApi

# View migrations for specific context
dotnet ef migrations list --context TaskDbContext --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApi
dotnet ef migrations list --context ConversationDbContext --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApi
```

**Why Both projects?**: Infrastructure contains DbContexts, but WebApi has configuration (`appsettings.json` connection strings) and DI setup.

**Migration Organization**:

- `Migrations/TaskDb/` - SQL Server migrations
- `Migrations/ConversationDb/` - PostgreSQL migrations

**Connection Strings** (in `appsettings.Development.json`):

```json
{
  "ConnectionStrings": {
    "TasksConnection": "Server=localhost;Database=TaskAgentDb;Trusted_Connection=true;",
    "ConversationsConnection": "Host=localhost;Port=5432;Database=taskagent_conversations;Username=postgres;Password=your-password"
  }
}
```

**Automatic Migrations on Startup**:

The application **automatically applies pending migrations** on startup for **BOTH databases** in ALL environments via:

```csharp
// Program.cs
await app.ApplyDatabaseMigrationsAsync();
```

This calls `DatabaseMigrationService.ApplyDatabaseMigrationsAsync()` which:

- Applies SQL Server migrations (`TaskDbContext`)
- Applies PostgreSQL migrations (`ConversationDbContext`)
- **Fails fast** if either database is unavailable (logs error and throws)
- Prevents application startup with incomplete database infrastructure

**Why fail-fast?**: Both databases are critical - SQL Server for task operations, PostgreSQL for conversation persistence. Without both, the application cannot function correctly (no direct REST endpoints for tasks outside chat).

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
dotnet run --project src/backend/services/TaskAgent/src/TaskAgent.WebApi

# Or from src/backend/services/TaskAgent/src/
dotnet run --project TaskAgent.WebApi
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

- Middleware: `WebApi/Middleware/ContentSafetyMiddleware.cs`
- Service: `Infrastructure/Services/ContentSafetyService.cs`
- Extension: `app.UseContentSafety()` in `Program.cs` (before `UseAuthorization()`)
- HttpClient: Named client via `IHttpClientFactory` (registered in `Infrastructure/InfrastructureServiceExtensions.cs`)

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
- **Controllers**: Use `ErrorResponseFactory` for standardized responses - `BadRequest(400)` for validation, `InternalServerError(500)` for unexpected errors
- **Middleware**: Log errors, fail open (allow request to proceed) for availability

**ErrorResponseFactory Pattern**:

```csharp
// ✅ GOOD - Standardized error responses
return ErrorResponseFactory.CreateBadRequest("ValidationError", "Invalid input", details);
return ErrorResponseFactory.CreateInternalServerError("Processing failed", exception.Message);
```

### Constants Pattern

All magic strings/numbers in constant files:

- `Domain/Constants/TaskConstants.cs`: MAX_TITLE_LENGTH (200)
- `Domain/Constants/ValidationMessages.cs`: Validation error messages
- `Application/Constants/ErrorMessages.cs`: Function tool error messages
- `Infrastructure/Constants/ContentSafetyConstants.cs`: API paths, HTTP client name
- `WebApi/Constants/ApiRoutes.cs`: Route constants (CHAT = "api/chat")
- `WebApi/Constants/ErrorCodes.cs`: Error codes for API responses
- `WebApi/Constants/ErrorMessages.cs`: User-facing error messages

### Dependency Injection Pattern

Each layer has a service extension class with extension methods:

```csharp
// Application/ApplicationServiceExtensions.cs
services.AddApplication();                  // Registers telemetry (AgentMetrics singleton)

// Infrastructure/InfrastructureServiceExtensions.cs
services.AddInfrastructure(configuration);  // Registers DbContext, Repositories, ContentSafetyClient

// WebApi/PresentationServiceExtensions.cs
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
- **SQL Server**: localhost via `"Server=localhost;Database=TaskAgentDb;Trusted_Connection=true;"`
- **PostgreSQL**: `"Host=localhost;Port=5432;Database=taskagent_conversations;Username=postgres;Password=..."`
- **JSON Storage**: PostgreSQL `json` type (not `jsonb`) preserves property order for System.Text.Json polymorphism

### Azure OpenAI Client Setup

```csharp
// Registered in WebApi/PresentationServiceExtensions.cs
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
❌ `JsonSerializer.Serialize(threadJson)` → Use `threadJson.GetRawText()` to preserve property order  
❌ PostgreSQL `jsonb` type → Use `json` type to preserve `$type` property order

## Key Files Reference

| File                                                          | Purpose                                               |
| ------------------------------------------------------------- | ----------------------------------------------------- |
| `Program.cs`                                                  | Application entry, middleware pipeline, DI bootstrap  |
| `WebApi/PresentationServiceExtensions.cs`                     | WebApi layer services, AIAgent factory registration   |
| `WebApi/Services/TaskAgentService.cs`                         | Agent factory method, 170-line instructions, chat API |
| `WebApi/Services/ErrorResponseFactory.cs`                     | Standardized error response creation (400, 500)       |
| `Application/Functions/TaskFunctions.cs`                      | 6 function tools for AI agent                         |
| `Application/DTOs/ChatMessage.cs`                             | ConversationMessage concrete implementation           |
| `Domain/Entities/TaskItem.cs`                                 | Core entity, factory method, business rules           |
| `Domain/Entities/ConversationThread.cs`                       | Conversation entity with JSON storage                 |
| `Infrastructure/Data/ConversationDbContext.cs`                | PostgreSQL DbContext for conversation threads         |
| `Infrastructure/Services/ContentSafetyService.cs`             | 2-layer content safety (Prompt Shield + Moderation)   |
| `Infrastructure/Services/PostgresThreadPersistenceService.cs` | JSON blob persistence with metadata extraction        |
| `Infrastructure/Services/DatabaseMigrationService.cs`         | Auto-migration service (fail-fast strategy)           |
| `WebApi/Middleware/ContentSafetyMiddleware.cs`                | Middleware applying safety checks to `/api/chat`      |
| `Infrastructure/Data/TaskDbContext.cs`                        | SQL Server DbContext, task entity configurations      |
| `Infrastructure/Repositories/TaskRepository.cs`               | Repository pattern implementation                     |

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
<AspireVersion>13.0.0</AspireVersion>

# 2. Update global.json (must match!)
"Aspire.AppHost.Sdk": "13.0.0"

# 3. Restore
dotnet restore
```

**Key packages**:

- `Microsoft.Agents.AI.OpenAI` (1.0.0-preview.251110.2) - Agent Framework
- `Azure.AI.OpenAI` (2.1.0) - Azure OpenAI SDK
- `Azure.AI.ContentSafety` (1.0.0) - Content Safety SDK
- `Microsoft.EntityFrameworkCore.SqlServer` (10.0.0)
- `Aspire.Hosting.AppHost` (via `$(AspireVersion)` = 13.0.0) - .NET Aspire orchestration
- `SonarAnalyzer.CSharp` (10.15.0.120848) - Code quality analysis

**Why CPM?**: Ensures version consistency across all projects, prevents dependency conflicts, simplifies upgrades.

## MSBuild & Solution Structure

**Critical Understanding**: MSBuild configuration files are at `src/` level (NOT in `backend/` or `frontend/`):

- `src/Directory.Build.props` - Common project settings (target framework, Aspire version, code analysis)
- `src/Directory.Packages.props` - Centralized NuGet package versions
- `src/global.json` - SDK versions and MSBuild SDK versions

**Why at `src/`?**: MSBuild searches upward from each project directory. This location ensures BOTH:

1. `TaskAgent.AppHost` (at `src/TaskAgent.AppHost/`)
2. Backend projects (at `src/backend/services/TaskAgent/src/`)

...can access the same configuration without duplication.

**Solution Files**:

- `src/backend/TaskAgentWeb.sln` - Backend solution (4 projects: Domain, Application, Infrastructure, WebApp)
- AppHost is NOT in the backend solution - it's orchestration, not part of the backend

**Adding New Projects**:

When adding new backend projects, add them to `TaskAgentWeb.sln`:

```powershell
cd src/backend
dotnet sln add services/TaskAgent/src/TaskAgent.NewProject/TaskAgent.NewProject.csproj
```

## .NET Aspire & Observability Architecture

**Three-Project Aspire Structure**:

1. **`TaskAgent.AppHost`** - Orchestration layer (runs via `dotnet run --project src/TaskAgent.AppHost`)
   - Entry point: `AppHost.cs` with `builder.AddProject<Projects.TaskAgent_WebApi>("task-agent-webapi")`
   - Provides Aspire Dashboard at https://localhost:17198
   - Manages service lifecycle and discovery
2. **`TaskAgent.ServiceDefaults`** - Shared telemetry configuration (inside `src/backend/`)
   - `ServiceDefaultsExtensions.AddServiceDefaults()` registers OpenTelemetry, health checks, resilience
   - **Hybrid telemetry**: Auto-detects exporter based on environment
     - Development: `OTEL_EXPORTER_OTLP_ENDPOINT` → OTLP → Aspire Dashboard
     - Production: `APPLICATIONINSIGHTS_CONNECTION_STRING` → Azure Monitor → Application Insights
   - Registered custom sources: `"TaskAgent.Agent"`, `"TaskAgent.Functions"`
   - Security: HTTPS-only service discovery in production
3. **`TaskAgent.WebApi`** - Application (calls `builder.AddServiceDefaults()` in `Program.cs`)

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

**Architecture**: Dual-database system with **JSON blob storage** in PostgreSQL for conversations.

### Database Strategy

- **SQL Server** (`TaskDbContext`): Task entities (CRUD operations)
- **PostgreSQL** (`ConversationDbContext`): Conversation threads (JSON blobs)

**Critical**: Both databases **MUST be available** on startup - application fails fast if either is down.

### JSON Blob Pattern

**Implementation**: `PostgresThreadPersistenceService` (scoped) - stores complete `AgentThread` as single JSON document.

```csharp
// ConversationThread entity stores:
- ThreadId (PK)                    // Unique conversation identifier
- SerializedThread (json)          // Complete AgentThread as JSON
- Title (string, 50 chars)         // Auto-extracted from first user message
- Preview (string, 100 chars)      // Auto-extracted from last assistant message
- MessageCount (int)               // Total messages including function calls/results
- CreatedAt, UpdatedAt (timestamptz)
- IsActive (bool)
```

**Why PostgreSQL `json` type?** (not `jsonb`):

- Preserves property order (critical for `$type` first in JSON)
- System.Text.Json polymorphic deserialization requires `$type` as first property
- `jsonb` reorders alphabetically → breaks Microsoft Agent Framework deserialization
- Still get JSON validation + query capabilities

### Thread Serialization Pattern

**CRITICAL**: Use `GetRawText()` to preserve exact JSON structure:

```csharp
// ✅ CORRECT - Preserves $type property order
JsonElement threadJson = thread.Serialize();
string serialized = threadJson.GetRawText(); // Preserves original structure
await _threadPersistence.SaveThreadAsync(threadId, serialized);

// ❌ WRONG - JsonSerializer.Serialize() reorders properties
string serialized = JsonSerializer.Serialize(threadJson); // Breaks deserialization!
```

**Deserialization**:

```csharp
// Load without additional options - preserves structure
string serialized = await _threadPersistence.GetThreadAsync(threadId);
JsonElement json = JsonSerializer.Deserialize<JsonElement>(serialized);
AgentThread thread = _agent.DeserializeThread(json);
```

### Message Structure

Thread JSON structure: `{ storeState: { messages: [...] } }`

**Message types in array**:

- `role: "user"` - User input messages
- `role: "assistant"` + `$type: "functionCall"` - Agent function invocations (internal)
- `role: "tool"` - Function execution results (internal)
- `role: "assistant"` + `$type: "text"` - Final agent responses (visible)

**MessageCount semantics**: Includes ALL messages (user, functionCall, tool, text) - not just visible user/assistant exchanges.

### Metadata Extraction

`PostgresThreadPersistenceService.ExtractMetadataFromJson()` parses JSON to extract:

- **Title**: First user message content (max 50 chars + "...")
- **Preview**: Last assistant text message (max 100 chars + "...")
- **MessageCount**: Total messages in `storeState.messages` array

**Navigation path**: `root → storeState → messages → contents[0].text`

### Request Flow

```csharp
// 1. Load existing thread or create new
string? serializedThread = await _threadPersistence.GetThreadAsync(threadId);
AgentThread thread = string.IsNullOrEmpty(serializedThread)
    ? _agent.GetNewThread()
    : _agent.DeserializeThread(JsonSerializer.Deserialize<JsonElement>(serializedThread));

// 2. Process message with full conversation history
dynamic? response = await _agent.RunAsync(message, (dynamic)thread);

// 3. Serialize with GetRawText() and save
JsonElement updatedThreadJson = thread.Serialize();
string updatedThreadSerialized = updatedThreadJson.GetRawText();
await _threadPersistence.SaveThreadAsync(threadId, updatedThreadSerialized);
```

### Conversation History API

**Extract messages** for display (filters internal function calls):

```csharp
// TaskAgentService.ExtractMessagesFromThread()
// Returns only user and assistant text messages
// Skips: functionCall, functionResult (internal)
// Creates: ConversationMessage records for API response
```

**Pagination support**: `GetConversationHistoryAsync()` paginates extracted messages.

## Conversation Management API

**Backend provides 4 REST endpoints for conversation management** (see `ChatController.cs`):

### 1. Send Message

```http
POST /api/Chat/send
Content-Type: application/json

{
  "message": "Create a high priority task",
  "threadId": "optional-thread-id"  // Omit to create new conversation
}

Response: 200 OK
{
  "message": "✅ Task created successfully: ...",
  "threadId": "abc-123-def",
  "messageId": "msg-456",
  "timestamp": "2025-11-14T10:30:00Z",
  "functionCalls": [...],
  "suggestions": ["View all tasks", "Create another task"]
}
```

### 2. List Conversations

```http
GET /api/Chat/threads?page=1&pageSize=20&sortBy=UpdatedAt&sortOrder=desc&isActive=true

Response: 200 OK
{
  "threads": [
    {
      "id": "abc-123-def",
      "title": "Create a high priority task to review quarterl...",  // Auto-generated
      "preview": "✅ Task created successfully. I've added a high...",
      "messageCount": 5,
      "createdAt": "2025-11-14T10:00:00Z",
      "updatedAt": "2025-11-14T10:35:00Z",
      "isActive": true
    }
  ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 20,
  "totalPages": 3
}
```

**Title Generation Strategy** (backend):

- Automatically extracts first user message content
- Max 50 characters + "..." if truncated
- Fallback: "New conversation" if no user messages

### 3. Get Conversation History

```http
GET /api/Chat/threads/{threadId}/messages?page=1&pageSize=50

Response: 200 OK
{
  "messages": [
    {
      "id": "msg-123",
      "role": "user",
      "content": "Create a high priority task",
      "timestamp": "2025-11-14T10:30:00Z"
    },
    {
      "id": "msg-124",
      "role": "assistant",
      "content": "✅ Task created successfully...",
      "timestamp": "2025-11-14T10:30:05Z"
    }
  ],
  "threadId": "abc-123-def",
  "totalCount": 5,
  "page": 1,
  "pageSize": 50
}
```

### 4. Delete Conversation

```http
DELETE /api/Chat/threads/{threadId}

Response: 204 No Content
```

**Thread Persistence Service**:

- **Production**: `PostgresThreadPersistenceService` (scoped) - PostgreSQL with JSON blob storage
- **Methods**: `SaveThreadAsync()`, `GetThreadAsync()`, `ListThreadsAsync()`, `DeleteThreadAsync()`
- **Critical**: Uses `GetRawText()` for serialization to preserve JSON property order
- **Metadata**: Auto-extracts title, preview, messageCount from thread JSON structure

## Testing Guidance

**Content Safety**: See `CONTENT_SAFETY.md` for 75+ test cases including prompt injections, harmful content, edge cases, and troubleshooting.

**AI Agent**: Test via web UI at `https://localhost:5001` or POST to `/api/chat` endpoint with `{"message": "your message", "threadId": "optional-thread-id"}`

**Database**: Auto-created on first run. To reset: `dotnet ef database drop --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApi`

**Key Test Scenarios**:

1. Task creation with validation (title length, priority values)
2. Business rule enforcement (can't reopen completed tasks)
3. Thread persistence across multiple requests (same threadId)
4. Content safety blocking (prompt injection, harmful content)
5. Parallel safety checks performance (~200-400ms)

## Frontend Development

**Next.js Application**: Located in `src/frontend/task-agent-web/`

**Tech Stack**: Next.js 16, React 19, TypeScript, Tailwind CSS 4 (NO external component libraries)

**Working Directory**: `src/frontend/task-agent-web/`

**Common Commands**:

```powershell
# From src/frontend/task-agent-web/
pnpm install           # Install dependencies (MUST use pnpm, NOT npm/yarn)
pnpm dev              # Run dev server (http://localhost:3000)
pnpm build            # Build for production (static export)
pnpm start            # Start production server (uses PORT env var)
pnpm lint             # Run ESLint
```

**Architecture**:

- **Server Component**: `app/page.tsx` - Root page component
- **Client Component**: `components/chat/ChatInterfaceClient.tsx` - Main chat interface (non-streaming, optimistic updates)
- **Hooks**:
  - `hooks/use-chat.ts` - Chat state management (custom implementation, NO Vercel AI SDK)
  - `hooks/use-conversations.ts` - Conversation management (list, load, delete)
- **API Client**: `lib/api/chat-service.ts` - Backend API communication (4 endpoints)

**Integration with Backend**:

API calls to backend at `https://localhost:5001` (configurable via `NEXT_PUBLIC_API_URL`):

- `POST /api/Chat/send` - Send message, returns full response
- `GET /api/Chat/threads` - List conversations with pagination
- `GET /api/Chat/threads/{threadId}/messages` - Get conversation history
- `DELETE /api/Chat/threads/{threadId}` - Delete conversation

**Current Implementation** (Non-streaming):

- Thread persistence via `threadId` in requests
- Optimistic UI updates (user message added immediately)
- Rollback on error
- Conversation titles auto-generated from first user message (backend)
- `localStorage` persistence of current thread ID

**Key Features**:

- ✅ Real-time chat interface with message history
- ✅ **Conversation sidebar** - List, load, delete conversations with auto-generated titles
- ✅ **ChatGPT-inspired UI** - Full-height adaptive layout (centered welcome, fixed input)
- ✅ **Smart suggestions** - Clickable suggestion buttons from AI
- ✅ **Contextual loading** - Rotating status messages during processing
- ✅ **Content Safety UX** (v2.1) - Blocked messages appear in chat (not toasts), thread continuity maintained
- ✅ **Smart Title Updates** - Titles regenerate when first valid message sent after block
- ✅ **Optimized Sidebar** - Only reloads when title changes (flag-based efficiency)
- ✅ Markdown rendering with `react-markdown`
- ✅ Responsive design with Tailwind CSS 4 only
- ✅ TypeScript type safety (strict mode)
- ✅ Environment-based API configuration
- 🔜 Streaming responses (planned, see `STREAMING_ROADMAP.md`)

**Component Structure**:

```
components/
├── chat/                           # Chat interface components
│   ├── ChatInterface.tsx           # Main layout (adaptive behavior)
│   ├── ChatInterfaceClient.tsx     # Client wrapper (dynamic loading)
│   ├── ChatMessagesList.tsx        # Messages container (scrollable)
│   ├── ChatMessage.tsx             # Individual message bubble
│   ├── ChatInput.tsx               # Input field (icon-based send)
│   ├── ChatHeader.tsx              # Minimalist header (conditional)
│   ├── EmptyChatState.tsx          # Welcome state (centered)
│   ├── SuggestionsBar.tsx          # Clickable suggestion buttons
│   └── ErrorToast.tsx              # Error display
├── conversations/                  # Conversation management
│   ├── ConversationSidebar.tsx     # Sidebar layout
│   ├── ConversationList.tsx        # List of conversations
│   ├── ConversationItem.tsx        # Individual conversation card
│   └── DeleteConfirmModal.tsx      # Delete confirmation
└── shared/
    └── LoadingIndicator.tsx        # Contextual loading states
```

**Key Files**:

- `app/page.tsx` - Main page (server component)
- `app/layout.tsx` - Root layout with metadata
- `components/chat/ChatInterfaceClient.tsx` - Chat UI (client component)
- `hooks/use-chat.ts` - Chat state management (custom, no Vercel AI SDK)
- `hooks/use-conversations.ts` - Conversation management
- `lib/api/chat-service.ts` - API integration (4 endpoints)
- `next.config.ts` - Next.js configuration
- `tailwind.config.ts` - Tailwind CSS 4 configuration

**Important Design Decisions**:

1. **No pre-built component libraries** - Pure Tailwind CSS 4 for full control and smaller bundle
2. **No Vercel AI SDK** - Custom `fetch` implementation for non-streaming chat
3. **Conversation titles auto-generated** - Backend extracts first user message (max 50 chars)
4. **localStorage for thread ID** - Persists current conversation across page refreshes
5. **Static export** - `output: "export"` in next.config.ts for Azure Static Web Apps

**Future Aspire Integration**:

- AppHost will orchestrate both frontend (Next.js) and backend (.NET)
- Automatic service discovery and port management
- Unified observability across full stack

## Frontend Development Best Practices

### Package Manager - pnpm Required

**CRITICAL**: This Next.js project uses **pnpm** (not npm or yarn).

```powershell
# Install dependencies (uses pnpm-lock.yaml)
pnpm install

# DO NOT use npm or yarn - causes lock file conflicts
```

**Why pnpm?**: Faster, disk-efficient, strict dependency resolution.

### Next.js 16 + React 19 Patterns

**Server Components by Default**: All components are Server Components unless marked with `"use client"`.

```typescript
// ✅ GOOD - Server Component (default, no directive needed)
export default function TasksPage() {
  return <TaskList />; // No interactivity
}

// ✅ GOOD - Client Component (only when needed)
("use client");
export function ChatInput({ onSend }: Props) {
  const [message, setMessage] = useState(""); // Needs hooks
  // ...
}
```

**Custom Chat Implementation** (NO Vercel AI SDK):

This project uses a **custom `fetch` implementation** for chat functionality instead of Vercel AI SDK. This was intentional to maintain control over the request/response cycle.

```typescript
// hooks/use-chat.ts - Custom implementation
export function useChat(options: UseChatOptions = {}): UseChatReturn {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [threadId, setThreadId] = useState<string | null>(null);

  const handleSubmit = async (e: FormEvent) => {
    // Optimistic update: add user message immediately
    const newMessages = [...messages, userMessage];
    setMessages(newMessages);

    try {
      // Call backend API
      const response = await sendMessage(input, threadId);
      // Add assistant response
      setMessages([...newMessages, response]);
    } catch (error) {
      // Rollback on error
      setMessages(messages);
    }
  };
}
```

**Why Custom Implementation?**:

- Full control over non-streaming chat flow
- Easier error handling and rollback
- No external SDK dependencies
- Prepared for future SSE streaming migration (see `STREAMING_ROADMAP.md`)

**Conversation Management Hook**:

```typescript
// hooks/use-conversations.ts
export function useConversations(): UseConversationsReturn {
  const [conversations, setConversations] = useState<ConversationThread[]>([]);

  // List conversations with pagination
  const loadConversations = async (page = 1, pageSize = 20) => {
    const response = await listThreads(page, pageSize);
    setConversations(response.threads);
  };

  // Load specific conversation history
  const loadConversation = async (threadId: string) => {
    const response = await getConversation(
      threadId,
      1,
      PAGINATION.MAX_MESSAGES
    );
    return response.messages;
  };

  // Delete conversation
  const deleteConversation = async (threadId: string) => {
    await deleteThread(threadId);
    // Remove from local state
    setConversations((prev) => prev.filter((c) => c.id !== threadId));
  };
}
```

### Environment Variables (Next.js)

**Client-side variables** must be prefixed with `NEXT_PUBLIC_`:

```bash
# .env.local
NEXT_PUBLIC_API_URL=https://localhost:5001  # Exposed to browser
API_SECRET_KEY=secret123                    # Server-side only
```

**Usage**:

```typescript
// Client component - must use NEXT_PUBLIC_ prefix
const apiUrl = process.env.NEXT_PUBLIC_API_URL;

// Server component - can access any env var
const secretKey = process.env.API_SECRET_KEY;
```

### Cross-Platform npm Scripts

**CRITICAL**: Use `run-script-os` for cross-platform port handling:

```json
// package.json
{
  "scripts": {
    "start": "run-script-os",
    "start:win32": "next start -p %PORT%", // Windows env var syntax
    "start:default": "next start -p $PORT" // Unix env var syntax
  }
}
```

**Why?**: Windows uses `%PORT%`, Unix/macOS uses `$PORT` - `run-script-os` auto-detects platform.

## Version Constraints

**Critical Version Alignment**:

- **.NET SDK**: 10.0.0 (see `src/global.json`)
- **Aspire Version**: 13.0.0 (see `src/Directory.Build.props` `$(AspireVersion)`)
- **Aspire SDK**: 13.0.0 (see `src/global.json` `msbuild-sdks.Aspire.AppHost.Sdk`)
- **Target Framework**: net10.0 (all C# projects)

**These versions MUST match** - mismatch causes build failures.

**Updating Aspire** (see `src/ASPIRE_VERSION.md` for full guide):

```powershell
# 1. Update Directory.Build.props
<AspireVersion>13.0.0</AspireVersion>

# 2. Update global.json (must match step 1!)
"Aspire.AppHost.Sdk": "13.0.0"

# 3. Restore
dotnet restore
```

## Code Quality Standards

### SonarAnalyzer.CSharp

**All projects** automatically include SonarAnalyzer (via `Directory.Build.props`):

```xml
<!-- Automatic inclusion -->
<PackageReference Include="SonarAnalyzer.CSharp" />
```

**Enforced settings**:

- Code analysis level: `latest`
- Analysis mode: `All`
- Warnings as errors: `true` (for code analysis)
- Code style enforced in build: `true`

**Result**: High-quality code is enforced at compile time.

### Nullable Reference Types

**CRITICAL**: All projects have nullable reference types **enabled**:

```xml
<Nullable>enable</Nullable>
```

**Implications**:

```csharp
// ✅ GOOD - Explicit nullability
string? optionalValue = null;  // Can be null
string requiredValue = "text";  // Never null

// ❌ BAD - Warning: Converting null literal
string text = null;  // CS8600 warning

// ✅ GOOD - Null-checking before use
if (optionalValue != null) {
    Console.WriteLine(optionalValue.Length);  // Safe
}
```

**Best practice**: Always annotate nullable types with `?` operator.

## Application Versioning

**Version control**: `src/Directory.Build.props`

```xml
<Version>2.0.0</Version>
```

**Impact**:

- MSBuild auto-derives `FileVersion` and `AssemblyVersion`
- Update before creating Git tags/releases
- Single source of truth for all projects

**Example workflow**:

```powershell
# 1. Update version in Directory.Build.props
<Version>2.1.0</Version>

# 2. Build (version applied to all assemblies)
dotnet build

# 3. Create Git tag
git tag v2.1.0
```
