# TaskAgent - Backend API

AI-powered task management backend built with **.NET 10**, **Microsoft Agent Framework**, and **Azure OpenAI**. Implements Clean Architecture with production-grade observability and dual-database persistence.

## 🏗️ Architecture

**Clean Architecture** with strict dependency flow:

```
┌───────────────────────────────────────────────────────────────┐
│                      Presentation Layer                       │
│                    (TaskAgent.WebApi)                         │
│  • REST API Controllers (Chat, Task)                          │
│  • Content Safety Middleware                                  │
│  • Configuration Validation                                   │
│  • DI Registration                                            │
└───────────────────────────────────────────────────────────────┘
                            │ depends on
                            ▼
┌───────────────────────────────────────────────────────────────┐
│                    Infrastructure Layer                       │
│                 (TaskAgent.Infrastructure)                    │
│  • Database Contexts (SQL Server + PostgreSQL)                │
│  • Repositories (TaskRepository)                              │
│  • External Services (ContentSafetyService)                   │
│  • Thread Persistence (PostgresThreadPersistenceService)      │
└───────────────────────────────────────────────────────────────┘
                            │ depends on
                            ▼
┌───────────────────────────────────────────────────────────────┐
│                     Application Layer                         │
│                  (TaskAgent.Application)                      │
│  • DTOs (Request/Response models)                             │
│  • Interfaces (ITaskRepository, IThreadPersistenceService)    │
│  • Function Tools (6 AI agent functions)                      │
│  • Telemetry (AgentMetrics, AgentActivitySource)              │
└───────────────────────────────────────────────────────────────┘
                            │ depends on
                            ▼
┌───────────────────────────────────────────────────────────────┐
│                       Domain Layer                            │
│                    (TaskAgent.Domain)                         │
│  • Entities (TaskItem, ConversationThread)                    │
│  • Enums (TaskStatus, TaskPriority)                           │
│  • Business Rules & Validation                                │
│  • NO external dependencies                                   │
└───────────────────────────────────────────────────────────────┘
```

## 📂 Project Structure

```
src/
├── .editorconfig                              # C# code style rules (shared)
├── Directory.Build.props                      # ⭐ MSBuild properties (shared)
├── Directory.Packages.props                   # ⭐ Central Package Management
├── global.json                                # ⭐ .NET SDK + Aspire SDK versions
│
├── TaskAgent.AppHost/                         # .NET Aspire orchestration
│   ├── AppHost.cs                             # Orchestrates backend services
│   ├── appsettings.json                       # Aspire configuration
│   └── Properties/launchSettings.json         # Launch settings
│
└── backend/                                   # Backend services
    ├── TaskAgentWeb.sln                       # Visual Studio solution
    │
    ├── TaskAgent.ServiceDefaults/             # Shared telemetry config
    │   └── ServiceDefaultsExtensions.cs       # OpenTelemetry, health checks
    │
    └── services/TaskAgent/src/                # Clean Architecture layers
        │
        ├── TaskAgent.Domain/                  # 🟢 Core business logic (NO dependencies)
        │   ├── Constants/
        │   │   ├── ConversationThreadConstants.cs
        │   │   ├── TaskConstants.cs
        │   │   └── ValidationMessages.cs
        │   ├── Entities/
        │   │   ├── ConversationThread.cs      # Conversation metadata
        │   │   └── TaskItem.cs                # Task entity with factory method
        │   └── Enums/
        │       ├── TaskPriority.cs
        │       └── TaskStatus.cs
        │
        ├── TaskAgent.Application/             # 🟡 Use cases & interfaces
        │   ├── Constants/
        │   │   └── ErrorMessages.cs
        │   ├── DTOs/
        │   │   ├── ChatMessage.cs             # Message DTO
        │   │   ├── ChatRequest.cs             # Send message request
        │   │   ├── ChatResponse.cs            # Send message response
        │   │   ├── ConversationHistoryDTOs.cs # History request/response
        │   │   ├── ConversationThreadDTO.cs   # Thread metadata
        │   │   ├── ListThreadsDTOs.cs         # List threads request/response
        │   │   ├── ContentSafetyResult.cs     # Safety check result
        │   │   ├── ErrorResponse.cs           # Error response
        │   │   ├── MessageMetadata.cs         # Function call metadata
        │   │   └── PromptInjectionResult.cs   # Prompt Shield result
        │   ├── Functions/
        │   │   └── TaskFunctions.cs           # 6 AI function tools
        │   ├── Interfaces/
        │   │   ├── IContentSafetyService.cs
        │   │   ├── ITaskAgentService.cs
        │   │   ├── ITaskRepository.cs
        │   │   └── IThreadPersistenceService.cs
        │   ├── Telemetry/
        │   │   ├── AgentActivitySource.cs     # Distributed tracing
        │   │   └── AgentMetrics.cs            # Custom metrics
        │   └── ApplicationServiceExtensions.cs # DI registration
        │
        ├── TaskAgent.Infrastructure/          # 🔵 External concerns
        │   ├── Constants/
        │   │   └── ContentSafetyConstants.cs
        │   ├── Data/
        │   │   ├── ConversationDbContext.cs   # PostgreSQL context
        │   │   └── TaskDbContext.cs           # SQL Server context
        │   ├── Migrations/
        │   │   ├── ConversationDb/            # PostgreSQL migrations
        │   │   └── TaskDb/                    # SQL Server migrations
        │   ├── Models/
        │   │   ├── ContentSafetyConfig.cs
        │   │   └── PromptShieldResponse.cs
        │   ├── Repositories/
        │   │   └── TaskRepository.cs          # EF Core implementation
        │   ├── Services/
        │   │   ├── ContentSafetyService.cs         # Prompt Shield + Moderation
        │   │   ├── DatabaseMigrationService.cs     # Auto-migration on startup
        │   │   └── PostgresThreadPersistenceService.cs  # JSON blob storage
        │   └── InfrastructureServiceExtensions.cs # DI registration
        │
        └── TaskAgent.WebApi/                  # 🔴 Presentation layer (Web API)
            ├── Constants/
            │   ├── ApiRoutes.cs               # Route constants
            │   ├── ErrorCodes.cs              # Error code constants
            │   └── ErrorMessages.cs           # User-facing error messages
            ├── Controllers/
            │   └── ChatController.cs          # Chat API endpoints
            ├── Extensions/
            │   ├── ConfigurationValidationExtensions.cs
            │   └── ContentSafetyMiddlewareExtensions.cs
            ├── Middleware/
            │   └── ContentSafetyMiddleware.cs # 2-layer defense
            ├── Models/
            │   └── ChatRequestDto.cs          # API request model
            ├── Services/
            │   ├── TaskAgentService.cs        # AI agent orchestration
            │   └── ErrorResponseFactory.cs    # Standardized error responses
            ├── Properties/
            │   └── launchSettings.json        # Launch configuration
            ├── appsettings.json               # Base configuration
            ├── appsettings.Development.json   # Dev configuration
            ├── PresentationServiceExtensions.cs # DI registration
            └── Program.cs                     # Application entry point
```

## ✨ Key Features

### 1. Microsoft Agent Framework Integration

**Autonomous AI agent** with function calling capabilities:

```csharp
// Services/TaskAgentService.cs
public static AIAgent CreateAgent(
    AzureOpenAIClient azureClient,
    string modelDeployment,
    ITaskRepository taskRepository)
{
    var chatClient = azureClient.GetChatClient(modelDeployment);
    var taskFunctions = new TaskFunctions(taskRepository);

    // Create 6 function tools
    var createTaskTool = AIFunctionFactory.Create(taskFunctions.CreateTask);
    var listTasksTool = AIFunctionFactory.Create(taskFunctions.ListTasks);
    var getTaskDetailsTool = AIFunctionFactory.Create(taskFunctions.GetTaskDetails);
    var updateTaskTool = AIFunctionFactory.Create(taskFunctions.UpdateTask);
    var deleteTaskTool = AIFunctionFactory.Create(taskFunctions.DeleteTask);
    var getTaskSummaryTool = AIFunctionFactory.Create(taskFunctions.GetTaskSummary);

    // Create agent with 170-line instruction prompt
    var agent = chatClient.CreateAIAgent(
        instructions: GetInstructions(),
        tools: [createTaskTool, listTasksTool, ...]
    );

    return agent;
}
```

**6 Function Tools** (Application/Functions/TaskFunctions.cs):

1. `CreateTask` - Creates new tasks with validation
2. `ListTasks` - Lists tasks with optional filtering
3. `GetTaskDetails` - Retrieves specific task information
4. `UpdateTask` - Updates task properties
5. `DeleteTask` - Soft-deletes tasks
6. `GetTaskSummary` - Provides analytics by status/priority

**Key Principles**:

- ✅ All functions have `[Description]` attributes for AI understanding
- ✅ Return user-friendly strings (✅/❌ emojis) - **NEVER throw exceptions to AI**
- ✅ Internal exception handling with formatted error messages

### 2. Dual-Database Architecture

**SQL Server** for structured task data, **PostgreSQL** for flexible conversation storage:

```csharp
// Infrastructure/Data/TaskDbContext.cs - SQL Server
public class TaskDbContext : DbContext
{
    public DbSet<TaskItem> Tasks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Relational schema with indexes
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.Priority).HasConversion<int>();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Priority);
        });
    }
}

// Infrastructure/Data/ConversationDbContext.cs - PostgreSQL
public class ConversationDbContext : DbContext
{
    public DbSet<ConversationThread> ConversationThreads { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // JSON blob storage with metadata
        modelBuilder.Entity<ConversationThread>(entity =>
        {
            entity.HasKey(e => e.ThreadId);
            entity.Property(e => e.ThreadId).HasMaxLength(100);
            entity.Property(e => e.SerializedThread)
                  .HasColumnType("json");  // NOT jsonb - preserves property order
            entity.Property(e => e.Title).HasMaxLength(50);
            entity.Property(e => e.Preview).HasMaxLength(100);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.UpdatedAt);
        });
    }
}
```

**Why JSON type (not JSONB)?** Preserves property order for `System.Text.Json` polymorphic deserialization. Microsoft Agent Framework requires `$type` as first property.

### 3. Thread Persistence Pattern

**Serialization with GetRawText()** (preserves structure):

```csharp
// Services/TaskAgentService.cs
public async Task<ChatResponse> SendMessageAsync(ChatRequest request)
{
    // 1. Load or create thread
    string? serializedThread = await _threadPersistence.GetThreadAsync(request.ThreadId);
    AgentThread thread = string.IsNullOrEmpty(serializedThread)
        ? _agent.GetNewThread()
        : _agent.DeserializeThread(JsonSerializer.Deserialize<JsonElement>(serializedThread));

    // 2. Process message
    await thread.AddUserMessageAsync(request.Message);
    dynamic? response = await _agent.RunAsync((dynamic)thread);

    // 3. Serialize and save (CRITICAL: Use GetRawText())
    JsonElement updatedThreadJson = thread.Serialize();
    string updatedThreadSerialized = updatedThreadJson.GetRawText();  // ✅ Preserves property order
    await _threadPersistence.SaveThreadAsync(currentThreadId, updatedThreadSerialized);

    return response;
}
```

**Metadata Extraction** (automatic title/preview):

```csharp
// Services/PostgresThreadPersistenceService.cs
private (string Title, string Preview, int MessageCount) ExtractMetadataFromJson(string serializedThread)
{
    var json = JsonDocument.Parse(serializedThread);

    // Navigate: root → storeState → messages
    if (!json.RootElement.TryGetProperty("storeState", out var storeState))
        return ("New conversation", "", 0);

    if (!storeState.TryGetProperty("messages", out var messages))
        return ("New conversation", "", 0);

    var messagesArray = messages.EnumerateArray().ToList();
    int messageCount = messagesArray.Count;

    // Extract title from first user message
    var firstUserMessage = messagesArray
        .FirstOrDefault(m => m.GetProperty("role").GetString() == "user");
    string title = ExtractTextFromMessage(firstUserMessage);
    title = title.Length > 50 ? title.Substring(0, 47) + "..." : title;

    // Extract preview from last assistant text message
    var lastAssistantMessage = messagesArray
        .LastOrDefault(m => m.GetProperty("role").GetString() == "assistant"
                         && m.TryGetProperty("$type", out var type)
                         && type.GetString() == "text");
    string preview = ExtractTextFromMessage(lastAssistantMessage);
    preview = preview.Length > 100 ? preview.Substring(0, 97) + "..." : preview;

    return (title, preview, messageCount);
}
```

### 4. Content Safety Middleware

**2-layer parallel defense** on `/api/Chat/send`:

```csharp
// Middleware/ContentSafetyMiddleware.cs
public async Task InvokeAsync(HttpContext context)
{
    if (!context.Request.Path.StartsWithSegments("/api/Chat/send"))
    {
        await _next(context);
        return;
    }

    var request = await JsonSerializer.DeserializeAsync<ChatRequest>(context.Request.Body);

    // Parallel validation (~50% faster than sequential)
    var promptShieldTask = _contentSafety.ValidatePromptShieldAsync(request.Message);
    var contentModerationTask = _contentSafety.ValidateContentAsync(request.Message);

    await Task.WhenAll(promptShieldTask, contentModerationTask);

    // Check results (priority: injection first, then content)
    var promptResult = await promptShieldTask;
    if (promptResult.IsBlocked)
    {
        context.Response.StatusCode = 400;
        await JsonSerializer.SerializeAsync(context.Response.Body, promptResult);
        return;
    }

    var contentResult = await contentModerationTask;
    if (contentResult.IsBlocked)
    {
        context.Response.StatusCode = 400;
        await JsonSerializer.SerializeAsync(context.Response.Body, contentResult);
        return;
    }

    await _next(context);
}
```

**ContentSafetyService** implements both layers:

**Security Enhancements**:

- ✅ Blocked messages create thread placeholders for conversation continuity
- ✅ `SaveBlockedMessageAsync(threadId)` creates/restores threads WITHOUT persisting blocked content
- ✅ Security measure: Blocked message content is NEVER stored in database
- ✅ Thread title regeneration when first valid message arrives
- ✅ Backend automatically extracts title from first user message

**For detailed testing**: See [docs/CONTENT_SAFETY.md](../../../../../docs/CONTENT_SAFETY.md) with 75+ test cases

```csharp
// Services/ContentSafetyService.cs
public async Task<PromptInjectionResult> ValidatePromptShieldAsync(string userPrompt)
{
    // Layer 1: Prompt Shield (REST API)
    var request = new
    {
        userPrompt,
        documents = Array.Empty<string>()  // No system context (reduces false positives)
    };

    var response = await _httpClient.PostAsJsonAsync("/contentsafety/text:shieldPrompt?api-version=2024-09-01", request);
    // Parse and return result
}

public async Task<ContentSafetyResult> ValidateContentAsync(string text)
{
    // Layer 2: Content Moderation (SDK)
    var request = new AnalyzeTextOptions(text);
    var response = await _client.AnalyzeTextAsync(request);

    // Check against thresholds (Hate, Violence, Sexual, SelfHarm)
    // Return blocked if any category exceeds configured severity
}
```

### 5. Production Observability

**Custom telemetry** with OpenTelemetry:

```csharp
// Application/Telemetry/AgentMetrics.cs
public class AgentMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _requestCounter;
    private readonly Counter<long> _functionCallCounter;
    private readonly Counter<long> _errorCounter;
    private readonly Histogram<double> _responseDurationHistogram;

    public AgentMetrics()
    {
        _meter = new Meter("TaskAgent.Agent", "1.0.0");
        _requestCounter = _meter.CreateCounter<long>("agent.requests", "requests", "Total agent requests");
        _functionCallCounter = _meter.CreateCounter<long>("agent.function_calls", "calls", "Function tool invocations");
        _errorCounter = _meter.CreateCounter<long>("agent.errors", "errors", "Agent errors");
        _responseDurationHistogram = _meter.CreateHistogram<double>("agent.response.duration", "ms", "Response duration");
    }

    public void RecordRequest(string threadId, string status)
        => _requestCounter.Add(1, new KeyValuePair<string, object?>("thread.id", threadId),
                                   new KeyValuePair<string, object?>("status", status));

    public void RecordFunctionCall(string functionName, string status)
        => _functionCallCounter.Add(1, new KeyValuePair<string, object?>("function.name", functionName),
                                       new KeyValuePair<string, object?>("status", status));
}

// Application/Telemetry/AgentActivitySource.cs
public static class AgentActivitySource
{
    private static readonly ActivitySource Source = new("TaskAgent.Agent", "1.0.0");

    public static Activity? StartMessageActivity(string threadId, string message)
    {
        var activity = Source.StartActivity("Agent.ProcessMessage", ActivityKind.Internal);
        activity?.SetTag("thread.id", threadId);
        activity?.SetTag("message.length", message.Length);
        return activity;
    }

    public static Activity? StartFunctionActivity(string functionName)
    {
        var activity = Source.StartActivity($"Function.{functionName}", ActivityKind.Internal);
        activity?.SetTag("function.name", functionName);
        return activity;
    }
}
```

**Hybrid telemetry** (dev + production):

```csharp
// TaskAgent.ServiceDefaults/ServiceDefaultsExtensions.cs
public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
{
    // Automatic exporter selection
    var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
    var appInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

    builder.Services.AddOpenTelemetry()
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter("TaskAgent.Agent"))  // Custom metrics
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation(options =>
            {
                // Disable SQL capture in production (security)
                options.SetDbStatementForText = !builder.Environment.IsProduction();
            })
            .AddSource("TaskAgent.Agent")  // Custom traces
            .AddSource("TaskAgent.Functions"));

    // Development: OTLP → Aspire Dashboard
    if (!string.IsNullOrEmpty(otlpEndpoint))
    {
        builder.Services.AddOpenTelemetry().UseOtlpExporter();
    }

    // Production: Azure Monitor → Application Insights
    if (!string.IsNullOrEmpty(appInsightsConnectionString))
    {
        builder.Services.AddOpenTelemetry().UseAzureMonitor();
    }

    return builder;
}
```

## 🔌 API Endpoints

### Chat Endpoints

#### 1. Send Message

```http
POST /api/Chat/send
Content-Type: application/json

{
  "message": "Create a high priority task to review quarterly reports",
  "threadId": "abc-123-def"  // Optional, creates new if null
}

Response: 200 OK
{
  "message": "✅ Task created successfully: Review quarterly reports (High priority, Pending status)",
  "threadId": "abc-123-def",
  "messageId": "msg-456",
  "createdAt": "2025-11-17T10:30:00Z",
  "suggestions": [
    "View all tasks",
    "Create another task",
    "Show high priority tasks"
  ],
  "metadata": {
    "functionCalls": [
      {
        "functionName": "CreateTask",
        "arguments": {
          "title": "Review quarterly reports",
          "description": "",
          "priority": "High"
        },
        "result": "✅ Task created successfully (ID: 42)"
      }
    ]
  }
}
```

#### 2. List Conversations

```http
GET /api/Chat/threads?page=1&pageSize=20&sortBy=UpdatedAt&sortOrder=desc&isActive=true

Response: 200 OK
{
  "threads": [
    {
      "id": "abc-123-def",
      "title": "Create a high priority task to review quarterl...",
      "preview": "✅ Task created successfully. I've added a high...",
      "messageCount": 5,
      "createdAt": "2025-11-17T10:00:00Z",
      "updatedAt": "2025-11-17T10:35:00Z",
      "isActive": true
    }
  ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 20,
  "totalPages": 3
}
```

#### 3. Get Conversation History

```http
GET /api/Chat/threads/{threadId}/messages?page=1&pageSize=50

Response: 200 OK
{
  "messages": [
    {
      "id": "msg-123",
      "role": "user",
      "content": "Create a high priority task",
      "timestamp": "2025-11-17T10:30:00Z"
    },
    {
      "id": "msg-124",
      "role": "assistant",
      "content": "✅ Task created successfully: Review quarterly reports",
      "timestamp": "2025-11-17T10:30:05Z"
    }
  ],
  "threadId": "abc-123-def",
  "totalCount": 5,
  "page": 1,
  "pageSize": 50
}
```

#### 4. Delete Conversation

```http
DELETE /api/Chat/threads/{threadId}

Response: 204 No Content
```

## ⚙️ Configuration

### appsettings.Development.json

```json
{
  "ConnectionStrings": {
    "TasksConnection": "Server=localhost;Database=TaskAgentDb;Trusted_Connection=true;",
    "ConversationsConnection": "Host=localhost;Port=5432;Database=taskagent_conversations;Username=postgres;Password=your-password"
  },
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
  },
  "OTEL_EXPORTER_OTLP_ENDPOINT": "http://localhost:4317" // Aspire Dashboard
}
```

## 🚀 Running the Backend

### Development (with .NET Aspire)

```powershell
# From repository root
cd src
dotnet run --project TaskAgent.AppHost

# Application: https://localhost:5001
# Aspire Dashboard: https://localhost:17198
```

### Standalone (without Aspire)

```powershell
# From backend directory
cd src/backend/services/TaskAgent/src
dotnet run --project TaskAgent.WebApi

# Application: https://localhost:5001
```

### Database Setup

**Automatic migrations** on startup (both databases):

```csharp
// Program.cs
await app.ApplyDatabaseMigrationsAsync();  // Applies SQL Server + PostgreSQL migrations
```

**Manual migrations** (if needed):

```powershell
# SQL Server migrations
cd src/backend/services/TaskAgent/src
dotnet ef migrations add MigrationName --context TaskDbContext --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApi --output-dir Migrations/TaskDb

# PostgreSQL migrations
dotnet ef migrations add MigrationName --context ConversationDbContext --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApi --output-dir Migrations/ConversationDb

# Apply migrations
dotnet ef database update --context TaskDbContext --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApi
dotnet ef database update --context ConversationDbContext --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApi
```

## 🧪 Testing

### Manual Testing

```powershell
# Send message
curl -X POST https://localhost:5001/api/Chat/send `
  -H "Content-Type: application/json" `
  -d '{"message": "Create a high priority task", "threadId": null}'

# List conversations
curl https://localhost:5001/api/Chat/threads?page=1&pageSize=10

# Get conversation history
curl https://localhost:5001/api/Chat/threads/abc-123-def/messages

# Delete conversation
curl -X DELETE https://localhost:5001/api/Chat/threads/abc-123-def
```

### Content Safety Testing

See [docs/CONTENT_SAFETY.md](../../../../docs/CONTENT_SAFETY.md) for 75+ test cases covering:

- Prompt injection attacks
- Harmful content (Hate, Violence, Sexual, Self-Harm)
- Edge cases and false positives
- Troubleshooting guide

## 📊 Monitoring

### Development (Aspire Dashboard)

Access: https://localhost:17198

**Features**:

- Real-time metrics visualization
- Distributed tracing with correlation
- Structured logging with scopes
- Dependency mapping

### Production (Application Insights)

**Key queries**:

```kusto
// Agent request metrics
customMetrics
| where name == "agent.requests"
| summarize RequestCount = sum(value) by bin(timestamp, 5m), tostring(customDimensions.status)
| render timechart

// Function call distribution
customMetrics
| where name == "agent.function_calls"
| summarize CallCount = sum(value) by tostring(customDimensions["function.name"])
| render piechart

// Response duration percentiles
customMetrics
| where name == "agent.response.duration"
| summarize
    P50 = percentile(value, 50),
    P95 = percentile(value, 95),
    P99 = percentile(value, 99)
    by bin(timestamp, 5m)
| render timechart

// Error rate
customMetrics
| where name == "agent.errors"
| summarize ErrorCount = sum(value) by bin(timestamp, 5m), tostring(customDimensions.error_type)
| render timechart
```

## 🔒 Security Best Practices

1. **Content Safety**: 2-layer defense (Prompt Shield + Content Moderation)
2. **Input Validation**: Zod-like validation via Data Annotations
3. **SQL Injection Prevention**: EF Core parameterized queries
4. **Secret Management**: Azure Key Vault (production)
5. **HTTPS Only**: Enforced in production
6. **CORS**: Configured for specific frontend origin
7. **SQL Statement Capture**: Disabled in production (sensitive data)

## 📚 Resources

- [Microsoft Agent Framework Docs](https://learn.microsoft.com/en-us/agent-framework/)
- [Azure OpenAI Documentation](https://learn.microsoft.com/en-us/azure/ai-foundry/openai/reference)
- [Azure AI Content Safety](https://learn.microsoft.com/en-us/azure/ai-services/content-safety/)
- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

## 📝 License

See [LICENSE](../../../../LICENSE) file in root directory.

## 🔗 Related

- **Frontend**: [Next.js Frontend README](../../../frontend/task-agent-web/README.md)
- **Root**: [TaskAgent README](../../../../README.md)
- **Articles**: [Article Series on C# Corner](../../../../README.md#-article-series)

---

**Built with ❤️ using Clean Architecture and Microsoft Agent Framework**
