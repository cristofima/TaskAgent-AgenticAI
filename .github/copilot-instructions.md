# Task Agent - Copilot Instructions

## Architecture

**Clean Architecture** with strict downward dependencies: `Domain` → `Application` → `Infrastructure` → `WebApp`

**Critical**: Domain has NO external dependencies. Application defines interfaces (e.g., `ITaskRepository`); Infrastructure implements them (`TaskRepository`).

## AI Agent Pattern

### Factory Method (TaskAgentService.CreateAgent)

```csharp
// Wires: AzureOpenAIClient + TaskFunctions (6 tools) + 170-line instruction prompt
var agent = chatClient.CreateAIAgent(instructions: "...", tools: [...]);
```

**Key Points**:

- Agent is **scoped per request** (DI in `DependencyInjection.AddPresentation()`) - never singleton
- Each conversation gets a **thread ID** for context persistence via `_threads` dictionary
- Instructions embed behavioral rules: immediate task creation, Markdown tables with emojis, contextual suggestions
- Tools created with `AIFunctionFactory.Create(taskFunctions.MethodName)`

### Function Tools Contract

All 6 methods in `TaskFunctions.cs` **must**:

- Decorate with `[Description]` for AI understanding
- Return user-friendly strings (✅/❌ emojis) - **NEVER throw exceptions to AI**
- Catch all exceptions internally, return error strings
- Use emoji patterns: 🔴 High, 🟡 Medium, 🟢 Low; ⏳ Pending, 🔄 InProgress, ✅ Completed

## Domain Patterns

### Entity Creation

```csharp
// ❌ NEVER: new TaskItem()
// ✅ ALWAYS: TaskItem.Create(title, description, priority)
```

- Private setters + factory method enforce validation (title ≤ 200 chars)
- Business rules in entity: `UpdateStatus()` blocks Completed → Pending transitions

## Critical Workflows

### EF Migrations (multi-project solution)

```powershell
# Always specify both projects:
dotnet ef migrations add MigrationName --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApp
dotnet ef database update --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApp
```

### Running

```powershell
dotnet run --project TaskAgent.WebApp
# Requires appsettings.Development.json: AzureOpenAI:{Endpoint,ApiKey,DeploymentName}
# ContentSafety config optional (gracefully degrades with console warning)
```

## Content Safety Middleware

**2-Layer Defense** on `/api/chat` endpoints (parallel execution):

1. **Prompt Injection** (Prompt Shield) → Blocks (returns 400)
2. **Content Safety** (Moderation) → Blocks if thresholds exceeded (hate/violence/sexual/self-harm, default=2)

**Performance Optimization**: Both layers execute in parallel using `Task.WhenAll` for ~50% faster response times (~200-400ms vs ~400-800ms sequential). Security priority maintained by checking injection result first.

Applied via `app.UseContentSafety()` before `UseAuthorization()`. Implementation in `Infrastructure/Services/ContentSafetyService.cs` using Azure Content Safety SDK.

## Project-Specific Conventions

### Naming Quirk

`TaskStatus` enum collision resolved with alias: `using DomainTaskStatus = TaskAgent.Domain.Enums.TaskStatus;`

### Error Handling Strategy

- **Domain**: Throw `ArgumentException` for validation
- **Function Tools**: NEVER throw - catch & return error strings to AI
- **Controllers**: `BadRequest(400)` for validation, `StatusCode(500)` for unexpected

### Configuration

Secrets in `appsettings.Development.json` (gitignored): `AzureOpenAI:ApiKey`, `ContentSafety:ApiKey`

## Integration Points

### EF Core Patterns

- **Read-only queries**: Always use `.AsNoTracking()` for performance
- **Enums**: Stored as ints in SQL Server with `.HasConversion<int>()`
- **Indexes**: On Status, Priority, CreatedAt for filtering performance

## Common Modifications

### Adding Function Tools

1. Method in `TaskFunctions.cs` with `[Description]` on method & parameters
2. Register in `CreateAgent()`: `AIFunctionFactory.Create(taskFunctions.NewMethod)`
3. Update agent instructions string if behavioral guidance needed

### Modifying Agent Behavior

Edit 170-line instruction string in `TaskAgentService.CreateAgent()` - single source of truth for agent behavior

### Entity Changes

1. Update `TaskItem` + factory/methods → 2. EF migration → 3. Update repository if needed

## Anti-Patterns

❌ `new TaskItem()` → Use `TaskItem.Create()` factory  
❌ Singleton agent → Must be scoped per request  
❌ Throw from function tools → Return error strings  
❌ Skip `AsNoTracking()` → Query performance degradation  
❌ Multiple instruction sources → Single string in `CreateAgent()`

## Key Files

`Program.cs` (DI), `TaskAgentService.cs` (170-line instructions), `TaskFunctions.cs` (6 tools), `ContentSafetyMiddleware.cs` (4 layers), `TaskItem.cs` (factory + business rules)
