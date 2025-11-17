# Git Commit Message Guide

## CRITICAL: Analyze Changed Files FIRST

**Before generating any commit message:**

1. **Look at the ACTUAL file paths** in staged changes
2. **Determine area from file location:**
   - Files in `src/frontend/` → `frontend/*`
   - Files in `src/backend/` → `backend/*`
   - Files in `.github/workflows/` → `ci`
   - Root config files → `build`
   - `.md` files → `docs`
3. **DO NOT reuse the previous commit's scope**
4. **Match scope to file locations, not assumptions**

**Example:**

```
Changed files:
- src/frontend/task-agent-web/hooks/use-chat.ts
- src/frontend/task-agent-web/types/chat.ts

❌ WRONG: feat(backend/Application): implement chat
✅ CORRECT: feat(frontend/hooks): implement chat hook
```

---

## Commit Message Format

```
type(scope): description

[optional body]

[optional footer]
```

---

## Types

### Primary Types:

- **feat** - NEW functionality that adds value
  - New component, hook, API endpoint
  - New user-facing feature
  - New capability that didn't exist before
- **fix** - Correcting BROKEN behavior

  - Bug that causes errors
  - Incorrect logic that produces wrong results
  - Broken functionality that needs repair

- **refactor** - Code restructuring, SAME external behavior
  - Extracting components/functions
  - Renaming for clarity
  - Reorganizing code structure
  - Improving architecture
  - NO new features, NO behavior changes

### Secondary Types:

- **perf** - Performance optimization (measurable improvement)
- **style** - Formatting, CSS, whitespace only
- **test** - Adding or fixing tests
- **docs** - Documentation ONLY (`.md` files)
- **ci** - CI/CD pipeline changes
- **build** - Dependencies, build configuration
- **chore** - Maintenance, cleanup

---

## Critical Type Decisions

### `feat` vs `refactor` - Decision Guide:

**Ask yourself: "Is there NEW functionality that users/developers can now do?"**

✅ **Use `feat` when:**

- Adding a new component that didn't exist
- Creating a new API endpoint
- Implementing a new hook
- Adding new types/interfaces for NEW features
- Users can now do something they couldn't before

❌ **Use `refactor` when:**

- Breaking down existing component into smaller ones
- Moving code to better locations
- Renaming for clarity
- Extracting duplicate code
- Improving code organization
- Same functionality, better structure

**Examples:**

```
# NEW chat component → feat
feat(frontend/components): add ChatMessage component

# Breaking existing ChatInterface into pieces → refactor
refactor(frontend/chat): extract message list to separate component

# NEW streaming support → feat
feat(frontend/hooks): add streaming support to use-chat

# Improving existing hook structure → refactor
refactor(frontend/hooks): simplify use-chat state management

# NEW API method → feat
feat(frontend/api): add sendMessage function

# Changing how existing API works internally → refactor
refactor(frontend/api): migrate from Vercel AI SDK to native fetch
```

### `docs` vs `feat(docs)` - NEVER use `feat(docs)`:

**RULE: If ONLY `.md` files changed → ALWAYS use `docs` type**

✅ **CORRECT:**

```
docs: update README with setup steps
docs(backend): add API documentation
docs(frontend): document component props
```

❌ **NEVER:**

```
feat(docs): add documentation  ← WRONG!
refactor(docs): update README  ← WRONG!
chore(docs): fix typos         ← WRONG!
```

**Exception:** If you're modifying code AND documentation:

```
feat(frontend/hooks): add use-chat hook

Added custom hook for chat state management with
streaming support.

Modified files (3):
- hooks/use-chat.ts: Implemented hook
- types/chat.ts: Added types
- README.md: Documented usage  ← Code + docs = feat
```

---

## Scopes (Monorepo)

### Backend Scopes:

- `backend/Domain` - Entities, value objects
- `backend/Application` - Use cases, DTOs, interfaces
- `backend/Application.Functions` - AI agent function tools
- `backend/Infrastructure` - Data access, external services
- `backend/Infrastructure.Services` - Service implementations
- `backend/WebApp` - Controllers, middleware
- `backend/WebApp.Middleware` - Middleware components

### Frontend Scopes:

- `frontend/components` - React components
- `frontend/hooks` - Custom hooks
- `frontend/api` - API client code
- `frontend/types` - TypeScript interfaces
- `frontend/chat` - Chat feature
- `frontend/styles` - CSS/Tailwind

### Shared/Root Scopes:

- `aspire` - Aspire orchestration
- `ci` - CI/CD workflows
- `build` - Build configuration
- `docs` - Documentation
- `scripts` - Utility scripts

---

## Rules

1. **Always prefix with `backend/` or `frontend/`** (except docs, ci, build, aspire, scripts)
2. Description: **lowercase**, **imperative**, **50 chars max**
3. Body: **past tense**, **wrap at 72 chars**
4. **Include body when:**
   - Multiple files changed (2+)
   - Change needs explanation
   - Complex refactoring
   - **Be specific about WHAT changed in each file**

---

## Body Guidelines

### When to Include Body:

✅ **Always include body for:**

- 2+ files modified
- Complex changes needing explanation
- Migration instructions
- Breaking changes

❌ **Body optional for:**

- Single file, obvious change
- Simple typo fixes
- Self-explanatory updates

### Body Structure:

```
type(scope): short description (50 chars)

[Blank line]

Detailed explanation in past tense (72 char wrap).
Explain WHAT changed and WHY.

Modified files (X):
- File1.tsx: What was changed here
- File2.ts: What was changed here
- File3.ts: What was changed here

[Optional footer]
Closes #123
```

### Body Writing Tips:

1. **First paragraph:** High-level summary of changes
2. **File list:** For 2-10 files, list each with description
3. **For >10 files:** Group by category/layer
4. **Use past tense:** "Added", "Implemented", "Fixed"
5. **Be specific:** What changed in each file

**Example for your current changes:**

```
feat(frontend/chat): enhance markdown rendering and API integration

Migrated chat implementation from Vercel AI SDK to custom
solution with improved type safety and direct backend integration.
Added support for GitHub Flavored Markdown rendering.

Modified files (6):
- globals.css: Enhanced markdown styles for chat messages
- ChatMessage.tsx: Added GFM and rehype-raw plugins
- ChatMessagesList.tsx: Updated to use new ChatMessage types
- use-chat.ts: Replaced Vercel AI SDK with custom implementation
- chat-service.ts: Added createThread and deleteThread methods
- chat.ts: Updated types to match backend API contract

This provides better control over streaming and type safety.
```

---

## Examples

### Backend Examples

```
feat(backend/Application.Functions): add task search tool

fix(backend/Domain): correct status transition validation

refactor(backend/Infrastructure): extract agent instructions builder

perf(backend/Infrastructure): optimize task queries with AsNoTracking
```

### Frontend Examples

```
feat(frontend/hooks): create use-chat custom hook

fix(frontend/components): resolve message rendering bug

refactor(frontend/chat): simplify message list logic

style(frontend/chat): improve mobile responsiveness
```

### Documentation

```
docs: update README setup instructions

docs(backend): add API endpoint documentation

docs(frontend): document component props
```

### With Body (Multi-file)

```
feat(frontend/chat): implement streaming messages

Added real-time streaming support for AI agent responses
using Server-Sent Events. Messages now display incrementally
as they are generated.

Modified files (3):
- ChatInterface.tsx: Added streaming display
- use-chat.ts: Integrated SSE handling
- types/chat.ts: Added streaming types

Closes #42
```

### Cross-cutting Changes

```
feat(backend+frontend): add task priority update

Backend changes (2 files):
- TaskController.cs: Added PATCH endpoint
- TaskFunctions.cs: Added UpdateTaskPriority method

Frontend changes (3 files):
- TaskList.tsx: Added priority selector
- task-client.ts: Added updatePriority API call
- types/task.ts: Added PriorityUpdateRequest interface
```

---

## Breaking Changes

### When to Mark as BREAKING CHANGE:

Use `!` suffix or `BREAKING CHANGE:` footer when:

1. **API changes requiring consumer updates:**
   - Removing/renaming public methods or endpoints
   - Changing request/response structures
   - Changing method signatures
2. **Major framework upgrades:**

   - .NET 9 → .NET 10
   - Next.js 15 → 16
   - React 18 → 19

3. **Configuration changes:**

   - Removing/renaming config keys
   - Adding required settings without defaults

4. **Database schema changes:**
   - Removing columns
   - Changing column types

### When NOT Breaking:

- Internal refactoring (no external impact)
- Adding optional parameters with defaults
- Bug fixes restoring intended behavior
- Performance improvements (same output)

### Breaking Change Examples:

```
feat(backend/api)!: change task response format

BREAKING CHANGE: Task API responses now include metadata object.
Response structure changed from:
{
  "id": 1,
  "title": "Task"
}

To:
{
  "id": 1,
  "title": "Task",
  "metadata": {
    "createdBy": "user@example.com"
  }
}

Clients must update response parsing.
```

```
build!: upgrade to .NET 10

BREAKING CHANGE: Project now targets .NET 10.
Consumers must:
- Install .NET 10 SDK
- Update target framework to net10.0

Enables C# 13 features and improved performance.
```

```
refactor!: rename ITaskService to ITaskAgentService

BREAKING CHANGE: Interface renamed for clarity.
All implementations and consumers must update references.

Migration:
- Update DI registrations
- Change interface references in constructors
- Update mock objects in tests
```

---

## Common Mistakes

### ❌ Mistake 1: Wrong Area (Cache Effect)

**Problem:** Reusing scope from previous commit without checking files.

```
Previous commit: feat(backend/Application): add search
Current files: src/frontend/hooks/use-chat.ts

❌ WRONG: feat(backend/Application): add chat feature
✅ CORRECT: feat(frontend/hooks): create use-chat hook
```

**Fix:** Always check actual file paths, ignore previous commits.

---

### ❌ Mistake 2: `feat` when it's actually `refactor`

**Problem:** Calling code restructuring a "feature".

```
Changed files:
- ChatMessage.tsx: Extracted from ChatInterface
- MessageList.tsx: Extracted from ChatInterface
- ChatInterface.tsx: Now uses new components

❌ WRONG: feat(frontend/components): add message components
✅ CORRECT: refactor(frontend/chat): extract message components

Reason: No NEW functionality, just better organization
```

**Ask:** "Can users do something NEW?"

- YES → `feat`
- NO, just better code → `refactor`

---

### ❌ Mistake 3: `refactor` when it's actually `feat`

**Problem:** Calling new functionality a "refactor".

```
Changed files:
- use-chat.ts: Added streaming support
- types/chat.ts: Added streaming types

❌ WRONG: refactor(frontend/hooks): update use-chat
✅ CORRECT: feat(frontend/hooks): add streaming support

Reason: Streaming is NEW functionality, not restructuring
```

**Ask:** "Did I add a NEW capability?"

- YES → `feat`
- NO, same features → `refactor`

---

### ❌ Mistake 4: Using `feat(docs)` or `refactor(docs)`

**Problem:** Wrong type for documentation-only changes.

```
❌ WRONG: feat(docs): add setup guide
❌ WRONG: refactor(docs): update README
❌ WRONG: chore(docs): fix typos

✅ CORRECT: docs: add setup guide
✅ CORRECT: docs: update README structure
✅ CORRECT: docs: fix typos in API documentation
```

**Rule:** ONLY `.md` files changed → Use `docs` type.

---

### ❌ Mistake 5: Missing Area Prefix

**Problem:** Forgetting `backend/` or `frontend/` prefix.

```
❌ WRONG: feat(Application): add tool
❌ WRONG: fix(hooks): resolve bug
❌ WRONG: refactor(Infrastructure): extract class

✅ CORRECT: feat(backend/Application.Functions): add tool
✅ CORRECT: fix(frontend/hooks): resolve state bug
✅ CORRECT: refactor(backend/Infrastructure): extract class
```

---

### ❌ Mistake 6: Too Vague Description

**Problem:** Description doesn't say WHAT changed.

```
❌ WRONG: feat(frontend): add functionality
❌ WRONG: fix(backend): fix bug
❌ WRONG: refactor: improve code
❌ WRONG: feat(frontend/chat): enhance chat message rendering and API integration

✅ CORRECT: feat(frontend/components): add ChatMessage component
✅ CORRECT: fix(frontend/hooks): resolve infinite loop in use-chat
✅ CORRECT: refactor(backend/Infrastructure): extract agent builder
✅ CORRECT: feat(frontend/chat): add markdown rendering support
```

**Be specific:** Name the component, function, or feature.

---

## Decision Tree

1. **Are ONLY .md files changed?** → `docs:`
2. **Are files in `.github/workflows/`?** → `ci:`
3. **Is this a major framework upgrade?** → `build!:` or `ci!:` (breaking)
4. **Are files in `src/frontend/`?** → Use `frontend/*` scope
5. **Are files in `src/backend/`?** → Use `backend/*` scope
6. **Does it add new user-facing functionality?** → `feat`
7. **Does it fix broken functionality?** → `fix`
8. **Does it improve performance?** → `perf`
9. **Does it restructure code (same behavior)?** → `refactor`
10. **Is it style/formatting only?** → `style`

---

## Quick Reference

### Most Common Patterns:

```
# Backend - New features
feat(backend/Application.Functions): add [function name] tool
feat(backend/WebApp): add [endpoint/middleware]
feat(backend/Domain): add [entity/value object]

# Backend - Fixes
fix(backend/Domain): correct [validation/business rule]
fix(backend/Infrastructure): resolve [data access issue]

# Backend - Refactoring
refactor(backend/Infrastructure): extract [class/method]
refactor(backend/Application): simplify [logic/structure]

# Frontend - New features
feat(frontend/components): add [component name]
feat(frontend/hooks): create [hook name]
feat(frontend/api): add [client method]

# Frontend - Fixes
fix(frontend/components): resolve [rendering issue]
fix(frontend/hooks): correct [state management]

# Frontend - Styling
style(frontend/chat): improve [layout/responsiveness]

# Documentation
docs: update [document name]
docs(backend): add [API/architecture] documentation
docs(frontend): document [component/hook usage]

# CI/CD
ci: add [workflow/pipeline]
ci: update [deployment/build] configuration

# Build
build: update [dependency name]
build!: upgrade to [framework version]
```
