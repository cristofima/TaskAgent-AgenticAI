# Lessons Learned

## Overview

This document captures the lessons learned, challenges, and trade-offs encountered during the development of the Task Agent project - an AI-powered task management system built with Microsoft Agent Framework and Clean Architecture.

---

## Project Architecture

### Clean Architecture (4 Layers)

The project implements **Clean Architecture** with strict dependency flow:

```
┌───────────────────────────────────────────────────────────────┐
│                      Presentation Layer                       │
│                    (TaskAgent.WebApi)                         │
│  • REST API Controllers                                       │
│  • SSE Streaming Services                                     │
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
│  • External Services (AgentStreamingService)                  │
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

**Packaging**: Monolithic deployment (single deployable unit), but with Clean Architecture separation for maintainability and testability.

---

## Content Safety Migration

### Background

The project originally implemented a **custom 2-layer defense architecture** using `Azure.AI.ContentSafety` SDK:

1. **Layer 1: Azure Prompt Shield (REST API)** - Custom REST calls to `/contentsafety/text:shieldPrompt`
2. **Layer 2: Azure Content Safety SDK** - `Azure.AI.ContentSafety` NuGet package for content moderation

This was **in addition to** Azure OpenAI's built-in content filtering at the model level.

### Migration Decision

**Decision**: Remove custom `Azure.AI.ContentSafety` implementation and rely solely on Azure OpenAI's built-in content filtering.

**Rationale**: Azure OpenAI's built-in filter already provides comprehensive protection:
- Hate speech detection
- Violence detection
- Sexual content detection
- Self-harm detection
- Prompt injection attacks (Jailbreak detection)

The custom implementation added:
- Maintenance overhead
- Additional Azure resource costs
- Configuration complexity
- Redundant validation (same content checked twice)

### Key Challenges in Content Safety Migration

#### 1. Understanding Azure OpenAI's Built-in Content Filtering

**Challenge:** Initially unclear whether Azure OpenAI's built-in content filtering was sufficient or if the separate Content Safety SDK provided additional capabilities.

**Resolution:** Research via Microsoft Learn documentation confirmed:

> "Azure OpenAI Service includes a content filtering system that works alongside core models. This system detects and takes action on specific categories of potentially harmful content in both input prompts and output completions."

**Key Insight:** The separate `Azure.AI.ContentSafety` SDK is designed for scenarios where you need:
- Content moderation **outside** of Azure OpenAI
- Custom category detection
- Image/multimodal content analysis
- Fine-grained threshold control beyond Azure OpenAI's settings

#### 2. Error Response Format Differences

**Challenge:** The custom middleware returned structured error responses (`ContentSafetyResult` DTOs), while Azure OpenAI returns HTTP 400 with `code: "content_filter"`.

**Resolution:** Created a new SSE event type (`CONTENT_FILTER`) to handle Azure OpenAI's error format gracefully:

```csharp
// Infrastructure/Services/AgentStreamingService.cs
catch (ClientResultException ex) when (IsContentFilterError(ex))
{
    _contentFilterException = new ContentFilterException(ex.Message);
    yield break;
}

private static bool IsContentFilterError(ClientResultException ex)
{
    return ex.Status == 400 && 
           ex.Message.Contains("content_filter", StringComparison.OrdinalIgnoreCase);
}
```

#### 3. UX Continuity for Blocked Messages

**Challenge:** The original implementation showed blocked messages as error toasts, breaking the conversation flow.

**Resolution:** Implemented ChatGPT-like UX where blocked messages appear as assistant responses:

```typescript
// Frontend: lib/api/chat-service.ts
if (event.type === "CONTENT_FILTER") {
  fullMessage = event.message || "I'm unable to assist with that request...";
  onTextChunk(fullMessage); // Display in chat, not as error
}
```

**Trade-off:** This approach treats content filter blocks as "successful" responses from the AI (just with a refusal message), which maintains conversation continuity but may mask the distinction between intentional refusals and policy blocks.

---

## Clean Architecture Lessons

## Trade-offs Analysis

### Benefits of Removing Custom Content Safety

| Benefit | Impact |
|---------|--------|
| **Reduced complexity** | Removed ~500 lines of middleware, service, and DTO code |
| **Fewer dependencies** | Removed `Azure.AI.ContentSafety` NuGet package |
| **Simplified configuration** | No separate Content Safety endpoint/API key required |
| **Lower cost** | No additional Azure Content Safety resource charges |
| **Reduced latency** | No pre-validation overhead before sending to OpenAI |

### Trade-offs

| Trade-off | Mitigation |
|----------|------------|
| **Less granular control** | Azure OpenAI portal allows threshold configuration per deployment |
| **No custom categories** | For task management, built-in categories are sufficient |
| **Can't moderate non-OpenAI content** | Not needed in this architecture (all chat goes through OpenAI) |
| **Error messages less detailed** | User-friendly messages are preferred anyway (security best practice) |

---

## Clean Architecture Challenges

### 1. Layer Dependency Discipline

**Challenge:** Maintaining strict dependency flow in a growing codebase. Easy to accidentally reference Infrastructure from Application layer.

**Solution:** 
- Project references enforce layer boundaries at compile time
- Each layer has its own `{Layer}ServiceExtensions.cs` for DI registration
- SonarAnalyzer enforces code quality rules

```csharp
// Application layer defines interfaces
public interface ITaskRepository { ... }

// Infrastructure layer implements them
public class TaskRepository : ITaskRepository { ... }
```

### 2. Scoped Services in Singleton Contexts

**Challenge:** The AI Agent is registered as singleton, but needs access to scoped services like `DbContext`.

**Solution:** Inject `IServiceProvider` and create scopes per-operation:

```csharp
// Application/Functions/TaskFunctions.cs
public class TaskFunctions
{
    private readonly IServiceProvider _serviceProvider;
    
    public async Task<string> CreateTaskAsync(string title, string description)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
        // Use repository with fresh DbContext per call
    }
}
```

### 3. Domain Layer Purity

**Challenge:** Keeping Domain layer free of external dependencies while still having rich validation.

**Solution:** 
- Factory methods encapsulate validation logic
- Private setters enforce invariants
- Constants for magic numbers/strings

```csharp
// Domain/Entities/TaskItem.cs
public class TaskItem
{
    public string Title { get; private set; }
    
    private TaskItem() { } // EF Core only
    
    public static TaskItem Create(string title, string description, TaskPriority priority)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException(ValidationMessages.TITLE_REQUIRED);
            
        if (title.Length > TaskConstants.MAX_TITLE_LENGTH)
            throw new ArgumentException(ValidationMessages.TITLE_TOO_LONG);
            
        return new TaskItem { Title = title, /* ... */ };
    }
}
```

---

## Dual Database Architecture Lessons

### Challenge: SQL Server + PostgreSQL Coexistence

**Background:** The project uses two databases:
- **SQL Server** - Task entities (structured CRUD operations)
- **PostgreSQL** - Conversation threads (JSON blob storage)

### Key Lessons

#### 1. Separate DbContexts for Each Database

```csharp
// Infrastructure/Data/TaskDbContext.cs - SQL Server
public class TaskDbContext : DbContext
{
    public DbSet<TaskItem> Tasks { get; set; }
}

// Infrastructure/Data/ConversationDbContext.cs - PostgreSQL
public class ConversationDbContext : DbContext
{
    public DbSet<ConversationThreadMetadata> Conversations { get; set; }
}
```

#### 2. PostgreSQL `json` vs `jsonb` Type

**Challenge:** `jsonb` reorders properties alphabetically, breaking polymorphic deserialization that requires `$type` as the first property.

**Solution:** Use `json` type (preserves order) instead of `jsonb`:

```csharp
// Entity configuration
entity.Property(e => e.SerializedThread)
    .HasColumnType("json"); // NOT jsonb!
```

#### 3. Thread Serialization Preservation

**Challenge:** `JsonSerializer.Serialize()` reorders properties, breaking deserialization.

**Solution:** Use `GetRawText()` to preserve exact JSON structure:

```csharp
// ✅ CORRECT - Preserves structure
JsonElement threadJson = thread.Serialize();
string serialized = threadJson.GetRawText();

// ❌ WRONG - Reorders properties
string serialized = JsonSerializer.Serialize(threadJson);
```

#### 4. Two Serialization Formats for Conversation State

**Challenge:** The project ended up with two different formats for `serializedState`:
- **Full AgentThread JSON** - From normal streaming flow (complex object with chat history)
- **Simple ThreadDbKey GUID** - From conversation list API (stored in PostgreSQL metadata)

When loading a conversation from sidebar and sending a new message, the backend received a GUID but expected full JSON, causing it to create a new conversation.

**Solution:** The `AgentStreamingService.DeserializeThread()` now detects both formats:

```csharp
// Check if string (ThreadDbKey) vs object (AgentThread JSON)
if (stateElement.ValueKind == JsonValueKind.String)
{
    // Simple GUID - load history from database
    _pendingThreadId = stateElement.GetString();
    return _agent.GetNewThread();
}
// Full JSON - deserialize normally
return _agent.DeserializeThread(stateElement);
```

**Frontend Fix:** Use `serializedState` from API response, not threadId:

```typescript
// ✅ CORRECT
setSerializedState(response.serializedState ?? null);

// ❌ WRONG - Used threadId directly
setSerializedState(threadId);
```

---

## Best Practices Identified

### 1. Leverage Platform Capabilities First

> **Lesson:** Before implementing custom solutions, investigate what the platform provides out-of-the-box.

Azure OpenAI's built-in content filtering was sufficient. The custom implementation added complexity without significant benefit.

### 2. SSE Events for Graceful Error Handling

> **Lesson:** For streaming protocols, use dedicated event types for different error categories rather than breaking the stream.

The `CONTENT_FILTER` SSE event pattern allows:
- Clear distinction between network errors and policy blocks
- Conversation continuity (thread state still sent)
- Frontend can handle each case appropriately

### 3. Pin Preview Package Versions

> **Lesson:** Preview NuGet packages can change without documentation. Always pin exact versions.

```xml
<!-- Directory.Packages.props -->
<PackageVersion Include="Microsoft.Agents.AI.OpenAI" Version="1.0.0-preview.251125.1" />
```

The Microsoft Agent Framework is in preview. Auto-updates can break your build without warning. Pin versions and test thoroughly before upgrading.

### 4. Dual Serialization Format Handling

> **Lesson:** When integrating multiple persistence systems, handle format differences gracefully at deserialization time.

**Problem:** The project has two conversation persistence mechanisms:
- **AG-UI `/agui` endpoint**: Uses `PostgresChatMessageStore` with simple `ThreadDbKey` (GUID string)
- **Custom `/api/agent/chat` endpoint**: Uses `AgentStreamingService` with full `AgentThread` JSON

When loading a conversation from the sidebar, the frontend receives a simple ThreadDbKey, but `AgentStreamingService.DeserializeThread()` expected full AgentThread JSON.

**Solution:** Detect format and handle both cases:

```csharp
// Infrastructure/Services/AgentStreamingService.cs
public object DeserializeThread(string? serializedState)
{
    JsonElement stateElement = JsonSerializer.Deserialize<JsonElement>(serializedState);
    
    // Check if it's a simple ThreadDbKey string (from loadConversation)
    if (stateElement.ValueKind == JsonValueKind.String)
    {
        _pendingThreadId = stateElement.GetString();
        return _agent.GetNewThread(); // Load history separately
    }
    
    // Full AgentThread JSON - deserialize normally
    return _agent.DeserializeThread(stateElement);
}
```

**Key Insight:** When the `serializedState` is a simple GUID, store it and load conversation history from PostgreSQL in `StreamResponseAsync()`:

```csharp
if (!string.IsNullOrEmpty(_pendingThreadId))
{
    List<ChatMessage> historyMessages = await LoadMessagesFromDatabaseAsync(_pendingThreadId);
    messageList = historyMessages.Concat(messageList).ToList();
}
```

### 5. Security Through Obscurity (Appropriate Here)

> **Lesson:** For content safety blocks, generic user-facing messages are preferred over detailed error information.

```typescript
const CONTENT_FILTER_MESSAGE = 
  "I'm unable to assist with that request as it may violate content policies. " +
  "Please try rephrasing your message.";
```

This prevents:
- Attackers from learning filter thresholds
- Users from crafting bypass attempts
- Exposure of internal error details

### 5. Documentation-Driven Development

> **Lesson:** Update documentation as part of the code change, not after.

The Content Safety migration touched 5 documentation files. Keeping docs in sync prevents:
- Developers following outdated patterns
- Configuration confusion
- Support burden from incorrect setup instructions

### 6. Central Package Management (CPM)

> **Lesson:** Use `Directory.Packages.props` for consistent dependency versions across projects.

```xml
<!-- Directory.Packages.props - Single source of truth -->
<PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />

<!-- Individual .csproj - No version needed -->
<PackageReference Include="Microsoft.EntityFrameworkCore" />
```

Benefits:
- No version mismatches between projects
- Easy upgrades (change one file)
- Clear audit trail of all dependencies

### 7. Fail-Fast Database Strategy

> **Lesson:** For applications with critical database dependencies, fail fast on startup.

```csharp
// Both databases MUST be available
await app.ApplyDatabaseMigrationsAsync(); // Throws if either fails
```

Why: This application cannot function without both SQL Server (tasks) and PostgreSQL (conversations). Silent degradation would cause confusing errors later.

---

## Technology-Specific Lessons

### AG-UI Protocol Integration

**Challenge:** Integrating Microsoft's AG-UI protocol with existing REST API architecture.

**Lessons:**
1. **Single endpoint mapping** - `app.MapAGUI("/agui", agent)` handles everything
2. **Message store factory pattern** - Pass factory function for per-thread persistence
3. **Tool injection via closure** - Capture `IServiceProvider` for scoped dependencies

### .NET Aspire Orchestration

**Challenge:** Managing multi-project debugging with databases.

**Lessons:**
1. **AppHost at root level** - Keep orchestrator separate from backend solution
2. **ServiceDefaults for shared config** - Telemetry, health checks, resilience
3. **Dashboard URL** - https://localhost:17198 for OTLP visualization

### Next.js Frontend Patterns

**Challenge:** TypeScript strict mode with API responses.

**Lessons:**
1. **Type guards for API responses** - Never trust `as` type assertions
2. **Error boundaries per route** - `error.tsx` for graceful failures  
3. **pnpm enforcement** - Lock file incompatibility with npm/yarn

---

## Future Considerations

### When to Re-Add Custom Content Safety

Consider adding `Azure.AI.ContentSafety` back if:

1. **Multi-modal content** - Need to analyze images or audio
2. **Non-OpenAI models** - Using models without built-in filtering
3. **Custom categories** - Need domain-specific content detection
4. **Regulatory requirements** - Need audit logs of all safety checks
5. **Pre-processing validation** - Want to reject before sending to OpenAI (cost savings)

### Monitoring Recommendations

With the migration complete, monitor:

1. **Azure OpenAI metrics** - Track content filter trigger rate in Azure portal
2. **Frontend analytics** - Count `CONTENT_FILTER` events received
3. **User feedback** - Watch for complaints about over-blocking

---

## CI/CD Pipeline Lessons

### GitHub Actions Architecture

#### 1. Node.js Runtime for Actions (Not for Your Code)

**Challenge:** Confusion about why GitHub Actions like `actions/checkout` require Node.js updates when your project is .NET.

**Key Insight:** GitHub Actions are written in JavaScript/TypeScript and run on Node.js - this is the Actions runtime, not your application runtime.

```
┌────────────────────────────────────────────────────────┐
│              GitHub Actions Runner (ubuntu-latest)     │
├────────────────────────────────────────────────────────┤
│  Node.js Runtime (for executing Actions themselves)    │
│  ┌─────────────────┐  ┌─────────────────┐              │
│  │ checkout@v5     │  │ setup-dotnet@v4 │              │
│  │ (JavaScript)    │  │ (JavaScript)    │              │
│  └─────────────────┘  └─────────────────┘              │
├────────────────────────────────────────────────────────┤
│  .NET SDK 10.0.x (for YOUR code)                       │
│  ┌─────────────────────────────────────┐               │
│  │ dotnet build, dotnet test, etc.     │               │
│  └─────────────────────────────────────┘               │
└────────────────────────────────────────────────────────┘
```

**Lesson:** Keep Actions updated (v4→v5) to avoid Node.js deprecation warnings, even though your .NET code doesn't use Node.js.

#### 2. dotnet restore Requires Project/Solution Files

**Challenge:** `dotnet restore "directory/"` fails with `MSB1003: Specify a project or solution file`.

**Root Cause:** The `dotnet restore` command does NOT accept directories - it requires explicit `.sln` or `.csproj` files.

```yaml
# ❌ WRONG - Directory path
- name: Restore
  run: dotnet restore "src/backend/services/TaskAgent/tests"

# ✅ CORRECT - Solution file path
- name: Restore
  run: dotnet restore "src/backend/TaskAgentWeb.sln"
```

**Lesson:** Always point to the solution file for restore/build operations. Define `SOLUTION_PATH` environment variable for consistency.

### Testcontainers in GitHub Actions

#### 3. Docker Availability on Runners

**Challenge:** Will Testcontainers work in CI? Does GitHub Actions have Docker?

**Key Insight:** Ubuntu runners (`ubuntu-latest`) come with Docker Engine preinstalled. Testcontainers works out-of-the-box.

| Runner | Docker Available | Testcontainers Support |
|--------|------------------|------------------------|
| `ubuntu-latest` | ✅ Preinstalled | ✅ Full support |
| `windows-latest` | ❌ Not available | ❌ Won't work |
| `macos-latest` | ⚠️ Requires setup | ⚠️ Limited |

**First Run Consideration:** Initial workflow execution may be slower due to Docker image pulls:
- SQL Server image: ~1.5GB
- PostgreSQL image: ~80MB

**Lesson:** Always use `ubuntu-latest` for integration tests with Testcontainers. Set adequate `timeout-minutes` (30+) for first runs.

### Code Coverage Aggregation

#### 4. Combining Multiple Coverage Reports

**Challenge:** How to show unified coverage metrics when tests are split across Domain, Application, and Infrastructure projects?

**Solution:** Use ReportGenerator to merge Cobertura XML files:

```yaml
- name: Generate Combined Coverage Report
  run: |
    dotnet tool install -g dotnet-reportgenerator-globaltool
    reportgenerator \
      -reports:"./TestResults/**/coverage.cobertura.xml" \
      -targetdir:"./TestResults/CoverageReport" \
      -reporttypes:"Html;JsonSummary;Cobertura"
```

**Output Formats:**
- `Html` - Interactive report for download
- `JsonSummary` - Parseable for GitHub Job Summary
- `Cobertura` - Combined XML for external tools

**Reading JSON Summary in Bash:**

```bash
LINE_COV=$(cat ./TestResults/CoverageReport/Summary.json | jq -r '.summary.linecoverage // 0')
echo "| Lines | ${LINE_COV}% |" >> $GITHUB_STEP_SUMMARY
```

**Lesson:** Use `jq` (preinstalled on Ubuntu runners) to parse JSON and display metrics in GitHub Job Summary tables.

### Action Version Management

#### 5. Keeping Actions Updated

**Challenge:** How to know when GitHub Actions need updates?

**Resolution:** Use Context7 MCP or check official GitHub documentation for latest versions.

| Action | Purpose | Current Stable |
|--------|---------|----------------|
| `actions/checkout` | Clone repository | v5 |
| `actions/setup-dotnet` | Install .NET SDK | v4 |
| `actions/upload-artifact` | Upload build artifacts | v4 |
| `actions/download-artifact` | Download artifacts | v5 |
| `azure/login` | Azure authentication | v2 |
| `azure/webapps-deploy` | Deploy to App Service | v2 |

**Lesson:** Check action versions periodically. Major version bumps (v4→v5) often include important changes like Node.js runtime updates.

---

## Frontend Testing Lessons

### React 19 Compatibility

#### 1. Testing Library Version Requirements

**Challenge:** Tests failed with cryptic errors after upgrading to React 19.

**Root Cause:** `@testing-library/react` versions below 16 are incompatible with React 19's new architecture.

```json
// ❌ WRONG - Will fail with React 19
"@testing-library/react": "^14.0.0"

// ✅ CORRECT - Required for React 19
"@testing-library/react": "^16.3.0"
```

**Lesson:** When using React 19, always use `@testing-library/react` v16+. The compatibility matrix is not always clearly documented.

#### 2. Testing forwardRef Components

**Challenge:** Testing a component that uses `forwardRef` for external focus control. TypeScript error: "Type 'RefObject<null>' is not assignable to type 'Ref<HTMLTextAreaElement>'".

**Symptom:**

```typescript
// ❌ FAILS - ref typing mismatch
const ref = { current: null };
render(<ChatInput ref={ref} {...props} />);
```

**Root Cause:** When passing refs to `forwardRef` components in tests, you need a properly typed React ref from `useRef`, not a plain object.

**Solution:** Create a wrapper component that uses `useRef`:

```typescript
// ✅ CORRECT - Wrapper with useRef
function TestWrapper({ onRef }: { onRef: (ref: HTMLTextAreaElement | null) => void }) {
  const ref = useRef<HTMLTextAreaElement>(null);

  useEffect(() => {
    onRef(ref.current);
  }, [onRef]);

  return <ChatInput ref={ref} {...defaultProps} />;
}

it('should be focusable via ref', () => {
  let textareaRef: HTMLTextAreaElement | null = null;
  render(<TestWrapper onRef={(ref) => { textareaRef = ref; }} />);
  
  expect(textareaRef).toBeInstanceOf(HTMLTextAreaElement);
  textareaRef?.focus();
  expect(textareaRef).toHaveFocus();
});
```

**Lesson:** For testing `forwardRef` components, use a wrapper component with `useRef` hook rather than plain object refs. This ensures proper TypeScript typing and React behavior.

### GitHub Actions for Frontend CI/CD

#### 3. pnpm Setup Order Matters

**Challenge:** Workflow failed with cache errors when using `cache: 'pnpm'` in `setup-node`.

**Root Cause:** `actions/setup-node` requires pnpm to be installed BEFORE it runs to detect the cache directory.

```yaml
# ❌ WRONG - Cache won't work
- uses: actions/setup-node@v4
  with:
    cache: 'pnpm'
- uses: pnpm/action-setup@v4

# ✅ CORRECT - pnpm must be installed first
- uses: pnpm/action-setup@v4
  with:
    version: 9
- uses: actions/setup-node@v4
  with:
    cache: 'pnpm'
    cache-dependency-path: src/frontend/task-agent-web/pnpm-lock.yaml
```

**Lesson:** Always install pnpm (`pnpm/action-setup`) BEFORE `setup-node` when using pnpm caching. Also specify `cache-dependency-path` for monorepo structures.

#### 4. GitHub Actions Has No Built-in Test Report Viewer

**Challenge:** Expected GitHub Actions to display test results like Azure DevOps does with its Test tab.

**Key Insight:** Unlike Azure DevOps, GitHub Actions does NOT have a built-in test report viewer. Test results must be handled differently.

| Feature | Azure DevOps | GitHub Actions |
|---------|--------------|----------------|
| Built-in test report viewer | ✅ Yes (Test tab) | ❌ No |
| Test annotations in code | ✅ Yes | ✅ Yes (with reporter) |
| Coverage visualization | ✅ Yes (Coverage tab) | ❌ No (artifacts only) |
| Job Summary markdown | ❌ No | ✅ Yes (`GITHUB_STEP_SUMMARY`) |

**Solution:** Combine multiple techniques:

1. **Job Summary** (`GITHUB_STEP_SUMMARY`) - Display metrics directly in workflow summary
2. **GitHub Reporter** - Generate code annotations for failures
3. **Artifacts** - Upload HTML reports for detailed viewing

```yaml
# Job Summary with coverage table
- name: Generate Unit Test Summary
  run: |
    echo "## 🧪 Unit Test Results" >> $GITHUB_STEP_SUMMARY
    echo "| Metric | Coverage |" >> $GITHUB_STEP_SUMMARY
    echo "|--------|----------|" >> $GITHUB_STEP_SUMMARY
    echo "| Lines | $(cat coverage/coverage-summary.json | jq -r '.total.lines.pct')% |" >> $GITHUB_STEP_SUMMARY
```

**Lesson:** GitHub Actions requires explicit configuration for test visualization. Use Job Summaries for quick metrics and artifacts for detailed reports.

#### 5. Playwright GitHub Reporter for Code Annotations

**Challenge:** Want test failures to appear as annotations directly in PR code, like Azure DevOps.

**Solution:** Configure Playwright to use the `github` reporter in CI:

```typescript
// playwright.config.ts
reporter: process.env.CI
  ? [['github'], ['html', { open: 'never' }], ['list']]
  : [['html', { open: 'never' }], ['list']],
```

**Result:** When E2E tests fail in CI, GitHub shows annotations directly on the affected code lines in PRs.

**Lesson:** The `github` reporter is specifically designed for GitHub Actions - it uses workflow commands to create annotations.

#### 6. Vitest Coverage JSON Summary

**Challenge:** Wanted to display coverage metrics in GitHub Job Summary, but only had HTML reports.

**Root Cause:** Vitest's `json` reporter creates `coverage-final.json` (detailed per-file), but Job Summary scripts need aggregated totals from `coverage-summary.json`.

```typescript
// ❌ INCOMPLETE - No summary file
reporter: ['text', 'json', 'html']

// ✅ COMPLETE - Includes summary for CI
reporter: ['text', 'json', 'json-summary', 'html']
```

**Generated files:**
- `json` → `coverage-final.json` (detailed, per-file)
- `json-summary` → `coverage-summary.json` (aggregated totals)

**Lesson:** Always include `json-summary` reporter when you need to extract coverage metrics programmatically (CI/CD, badges, etc.).

---

## Conclusion

This document captures key learnings from building TaskAgent-AgenticAI:

**Architecture Decisions:**
- 4-layer Clean Architecture provides excellent separation of concerns
- Monolithic deployment simplifies operations while maintaining code quality
- Dual-database strategy (SQL Server + PostgreSQL) works well for different data patterns

**Integration Lessons:**
- Azure OpenAI's built-in content filtering is production-ready
- Preview packages require careful version management
- Platform capabilities should be leveraged before custom implementations

**Code Quality:**
- Central Package Management prevents dependency conflicts
- Factory methods in Domain layer enforce invariants
- SSE events enable graceful error handling in streaming protocols

**CI/CD Lessons:**
- GitHub Actions run on Node.js runtime (separate from your application runtime)
- `dotnet restore` requires explicit `.sln` or `.csproj` files, not directories
- Ubuntu runners include Docker preinstalled (Testcontainers works out-of-the-box)
- ReportGenerator combines multiple coverage reports into unified metrics
- Keep GitHub Actions updated to avoid Node.js deprecation warnings

**Frontend Testing Lessons:**
- React 19 requires `@testing-library/react` v16+ (not documented clearly)
- Testing `forwardRef` components requires wrapper with `useRef` hook
- pnpm must be installed BEFORE `setup-node` for cache to work
- GitHub Actions has no built-in test viewer (use Job Summaries + artifacts)
- Playwright `github` reporter creates code annotations in PRs
- Vitest `json-summary` reporter needed for CI coverage metrics

---

## References

- [Azure OpenAI Content Filtering](https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/content-filter)
- [Azure AI Content Safety Overview](https://learn.microsoft.com/en-us/azure/ai-services/content-safety/overview)
- [Content Filter Configuration](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/content-filters)
- [Microsoft Agents Framework (Preview)](https://github.com/microsoft/agents)
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
