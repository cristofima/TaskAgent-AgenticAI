# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

**Backend - Microsoft Entra ID Authentication**
- JWT Bearer Authentication - `Microsoft.Identity.Web` v3.9.0 integration
- User Context Service - `IUserContext` interface with `HttpUserContext` implementation
- User Isolation - All task operations filtered by authenticated user's ID
- Swagger OAuth - Bearer token support in Swagger UI for API testing
- Database Migration - `AddUserIdToTasks` migration (UserId column with index)

**Frontend - MSAL Authentication**
- MSAL React Integration - `@azure/msal-browser` v4.27.0 + `@azure/msal-react` v3.0.23
- AuthProvider Component - Wraps app with MsalProvider for auth context
- AuthButton Component - Sign in/out button with user avatar and menu
- Bearer Token Injection - Automatic token acquisition for API calls
- Graceful Fallback - App runs in anonymous mode when auth not configured
- Environment Template - `.env.example` with auth configuration variables

### Changed

- Updated `TaskItem` entity with `UserId` property (450 chars, indexed)
- Updated `TaskFunctions` to accept `IUserContext` for user-scoped operations
- Updated `ITaskRepository` interface with `userId` parameter on all methods
- Updated `TaskRepository` to filter all queries by user
- Updated all unit tests to pass `userId` to factory methods

### Fixed

- Microsoft.OpenApi v2.x breaking changes - Updated Swagger security configuration for new namespace structure

## [2.1.0] - 2025-12-08

### Added

**Frontend**
- AG-UI Step Lifecycle Events - `STEP_STARTED`/`STEP_FINISHED` events for function calls
- Real-Time Status Updates - Server-driven `STATUS_UPDATE` SSE events during processing
- Dark Theme Support - System detection + manual toggle with next-themes
- Content Safety UX - Blocked messages appear in chat (not toasts), thread continuity preserved
- Keyboard Shortcuts - `Ctrl+K` (focus input), `Ctrl+Shift+N` (new chat), `Ctrl+B` (toggle sidebar)
- Enhanced Loading States - ChatGPT-like status + cursor in assistant message bubble
- Testing Infrastructure - Vitest + Playwright with 18 unit tests and 13 E2E tests

**Backend**
- Dynamic Status Messaging - Status auto-generated from `[Description]` attributes
- SSE Streaming Service - `SseStreamingService` with `STEP_STARTED`, `STATUS_UPDATE`, `STEP_FINISHED` events
- Function Description Provider - `FunctionDescriptionProvider` for multi-agent scalability
- Content Filter Detection - `CONTENT_FILTER` SSE event for Azure OpenAI violations
- Serilog Integration - Centralized logging with console, file, and OpenTelemetry sinks
- Testing Infrastructure - xUnit with 132 tests (Domain, Application, Infrastructure)

### Changed

- Upgraded .NET Aspire SDK to 13.0.2
- Updated OpenTelemetry and Microsoft.Extensions packages
- Enhanced CI/CD workflows with combined coverage reports and testing summaries

## [2.0.0] - 2025-11-19

### Added

**Frontend (Next.js 16 + React 19)**
- Full Chat Management - `ConversationSidebar`, `ConversationList`, `ConversationItem` components
- Delete Confirmation Modal - Smooth animations with React Portal
- ChatGPT-Inspired UI - Full-height adaptive layout with Tailwind CSS
- Smart Suggestions - Clickable suggestion buttons from AI agent
- localStorage Persistence - Remembers current thread ID across sessions
- Responsive Design - Collapsible sidebar, mobile-friendly layout

**Backend**
- PostgreSQL Thread Persistence - `PostgresThreadPersistenceService` with JSON storage
- Custom AG-UI Endpoint - `/api/agent/chat` with SSE streaming
- Dual-Database Architecture - SQL Server (tasks) + PostgreSQL (conversations)
- CORS Configuration - Support for Next.js frontend separation

### Changed

- Renamed `TaskAgent.WebApp` to `TaskAgent.WebApi`
- Reorganized migrations into `TaskDb/` and `ConversationDb/` folders
- Configured Next.js for static export (Azure Static Web Apps deployment)
- Created separate backend and frontend CI/CD pipelines

### Breaking Changes

- Project upgraded from .NET 9 to .NET 10
- Namespace changed from `TaskAgent.WebApp` to `TaskAgent.WebApi`

## [1.2.0] - 2025-10-28

### Added

- .NET Aspire Integration - Orchestration with Aspire Dashboard
- OpenTelemetry Stack - Full observability (tracing, metrics, logging)
- Automatic Database Migration - `DatabaseMigrationService` on startup
- Custom Telemetry - `AgentActivitySource` and `AgentMetrics`

### Changed

- Upgraded Aspire SDK to 9.5.2
- Moved Clean Architecture layers to `src/services/TaskAgent/`
- Initial CI/CD workflow for .NET deployment

## [1.1.0] - 2025-10-19

### Added

- In-Memory Thread Persistence - `InMemoryThreadPersistenceService`
- Release Notes System - PowerShell scripts with GitHub Copilot prompts
- EditorConfig - C# code style rules

### Changed

- Converted DTOs to immutable record types for thread-safety
- Applied proper `Async` suffix for async methods

## [1.0.0] - 2025-10-17

### Added

**Backend**
- Microsoft Agent Framework - AI-powered task management with function tools
- Azure OpenAI Integration - GPT-4o-mini with streaming responses
- Content Safety Middleware - Prompt Shield + Content Moderation (parallel execution)
- Clean Architecture - Domain, Application, Infrastructure, Presentation layers
- 6 Function Tools - `CreateTask`, `ListTasks`, `UpdateTaskStatus`, `UpdateTaskPriority`, `DeleteTask`, `GetTaskSummary`
- Entity Framework Core - SQL Server with EF Core 9
- Swagger/OpenAPI - API documentation

[Unreleased]: https://github.com/cristofima/TaskAgent-AgenticAI/compare/v2.0.0...HEAD
[2.0.0]: https://github.com/cristofima/TaskAgent-AgenticAI/compare/v1.2.0...v2.0.0
[1.2.0]: https://github.com/cristofima/TaskAgent-AgenticAI/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/cristofima/TaskAgent-AgenticAI/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/cristofima/TaskAgent-AgenticAI/releases/tag/v1.0.0
