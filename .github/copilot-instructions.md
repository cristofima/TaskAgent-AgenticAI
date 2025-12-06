# Task Agent - Copilot Instructions

## Quick Reference

**Common Commands** (from repository root):

```powershell
# Backend (with Aspire Dashboard)
dotnet run --project src/TaskAgent.AppHost

# Backend (standalone)
cd src/backend/services/TaskAgent/src
dotnet run --project TaskAgent.WebApi

# Frontend
cd src/frontend/task-agent-web
pnpm install  # First time only
pnpm dev

# EF Migrations (from src/backend/services/TaskAgent/src/)
dotnet ef migrations add MigrationName --context TaskDbContext --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApi --output-dir Migrations/TaskDb
dotnet ef database update --context TaskDbContext --project TaskAgent.Infrastructure --startup-project TaskAgent.WebApi
```

**Key URLs**:
- Frontend: http://localhost:3000
- Backend API: https://localhost:5001
- Aspire Dashboard: https://localhost:17198
- AG-UI endpoint: https://localhost:5001/agui

## Project Overview

AI-powered task management system built with **Microsoft Agent Framework** (preview), demonstrating autonomous AI agents with function calling, Clean Architecture, and production-grade observability.

**Tech Stack**: .NET 10, ASP.NET Core MVC, Azure OpenAI (GPT-4o-mini), Entity Framework Core, .NET Aspire 13.0.2, OpenTelemetry

**Frontend**: Next.js 16, React 19, TypeScript, Tailwind CSS 4, SSE streaming

## Architecture

**Clean Architecture** with strict downward dependencies: `Domain` → `Application` → `Infrastructure` → `WebApi`

**Critical**: Domain has NO external dependencies. Application defines interfaces (e.g., `ITaskRepository`); Infrastructure implements them.

```
src/
├── TaskAgent.AppHost/                  # Aspire orchestrator
├── backend/services/TaskAgent/src/     # Clean Architecture (.NET)
│   ├── TaskAgent.Domain/               # Entities, Enums, Constants (NO dependencies)
│   ├── TaskAgent.Application/          # DTOs, Interfaces, Functions (TaskFunctions.cs)
│   ├── TaskAgent.Infrastructure/       # EF Core, Repositories, Azure Services
│   └── TaskAgent.WebApi/               # Controllers, AG-UI setup, DI
└── frontend/task-agent-web/            # Next.js + SSE streaming
```

## AI Agent Pattern

Uses `Microsoft.Agents.AI.OpenAI` with **AG-UI Protocol** for SSE streaming.

**Key Files**:
- `WebApi/Extensions/AgentServiceExtensions.cs` - Agent factory with ChatMessageStore
- `WebApi/Constants/AgentInstructions.cs` - Single source for agent behavior
- `Application/Functions/TaskFunctions.cs` - 6 function tools (CreateTask, ListTasks, etc.)

**Critical Pattern - Scoped Dependencies**:
```csharp
// TaskFunctions takes IServiceProvider, creates scope per-call for fresh DbContext
using IServiceScope scope = _serviceProvider.CreateScope();
var repository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
```

**Function Tools Contract**: All methods in `TaskFunctions.cs` must:
- Use `[Description]` attributes for AI understanding
- Return user-friendly strings (✅/❌ emojis) - **NEVER throw exceptions**
- Catch all exceptions, return formatted error strings

## Domain Patterns

```csharp
// ❌ NEVER: new TaskItem() { Title = "..." }
// ✅ ALWAYS: TaskItem.Create(title, description, priority)

// Business rules in entity methods
task.UpdateStatus(TaskStatus.InProgress);  // Validates transitions
```

## API Endpoints

**SSE Streaming** (Frontend uses these):
- `POST /api/agent/chat` - Send message (SSE stream response)
- `GET /api/conversations` - List conversations
- `GET /api/conversations/{threadId}/messages` - Get history
- `DELETE /api/conversations/{threadId}` - Delete conversation

**SSE Event Types**: `TEXT_MESSAGE_START`, `TEXT_MESSAGE_CONTENT`, `TEXT_MESSAGE_END`, `TOOL_CALL_START`, `TOOL_CALL_RESULT`, `CONTENT_FILTER`, `THREAD_STATE`

## Dual Database Architecture

- **SQL Server** (`TaskDbContext`) - Task entities
- **PostgreSQL** (`ConversationDbContext`) - Conversation threads (JSON blob storage)

Both databases **MUST be available** on startup (fail-fast strategy).

See `docs/DUAL_DATABASE_ARCHITECTURE.md` for rationale.

## Content Safety

Uses **Azure OpenAI's built-in content filtering** (no separate SDK). When triggered:
1. Backend catches `ClientResultException` with `content_filter` code
2. Sends `CONTENT_FILTER` SSE event
3. Frontend displays friendly message in chat

See `docs/CONTENT_SAFETY.md` for 75+ test cases.

## Anti-Patterns

❌ `new TaskItem()` → Use `TaskItem.Create()` factory  
❌ Inject scoped DbContext into singleton agent → Use `IServiceProvider.CreateScope()`  
❌ Throw from function tools → Return user-friendly error strings  
❌ Skip `.AsNoTracking()` on read queries → Performance degradation  
❌ Magic strings/numbers → Use constants from appropriate layer  
❌ npm/yarn for frontend → Use `pnpm` (enforced by pnpm-lock.yaml)

## Version Constraints

**These versions MUST match** (see `src/global.json` and `src/Directory.Build.props`):
- .NET SDK: 10.0.0
- Aspire: 13.0.2
- Target Framework: net10.0

**Central Package Management**: All NuGet versions in `src/Directory.Packages.props`

## Key Files Reference

| File | Purpose |
|------|---------|
| `WebApi/Extensions/AgentServiceExtensions.cs` | AG-UI + ChatMessageStore setup |
| `WebApi/Constants/AgentInstructions.cs` | Agent behavior prompt |
| `Application/Functions/TaskFunctions.cs` | Function tools for AI |
| `Domain/Entities/TaskItem.cs` | Core entity with factory method |
| `Infrastructure/MessageStores/PostgresChatMessageStore.cs` | AG-UI persistence |

## Additional Documentation

For detailed information, see the `docs/` folder:
- `DUAL_DATABASE_ARCHITECTURE.md` - Why SQL Server + PostgreSQL
- `CONTENT_SAFETY.md` - Security testing guide (75+ test cases)
- `POSTGRESQL_MIGRATION.md` - Database setup guide
- `LESSONS_LEARNED.md` - Project-wide patterns and best practices
- `FRONTEND_E2E_TESTING.md` - Frontend testing scenarios
