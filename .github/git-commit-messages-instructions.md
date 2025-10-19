# GitHub Copilot Custom Instructions - Git Conventional Commit Messages

These instructions guide GitHub Copilot in generating Git commit messages that adhere to the Conventional Commits specification.

**I. Conventional Commits Specification:**

- "Generate commit messages that follow the Conventional Commits specification ([https://www.conventionalcommits.org/en/v1.0.0/](https://www.conventionalcommits.org/en/v1.0.0/))."
- "Structure commit messages with a type, an optional scope, and a description: `<type>[optional scope]: <description>`"
- "A complete commit message follows this structure:"

  ```
  <type>[optional scope]: <description>

  [optional body]

  [optional footer(s)]
  ```

- "Separate the header from the optional body and footer with a blank line."

**II. Commit Message Structure:**

- **Header:**
  - **Type:**
    - "Use one of the following types (in lowercase) based on these specific criteria:"
      - `feat`: **NEW FUNCTIONALITY** - Adding new features, endpoints, business logic, or capabilities that provide value to users. Examples:
        - Adding a new API endpoint (`/api/chat`, `/api/tasks`)
        - Adding new AI agent function tools (e.g., `DeleteTask`, `SearchTasks`)
        - Implementing new business rules (e.g., task priority auto-adjustment)
        - Adding new domain entities or value objects (e.g., `TaskComment`, `TaskHistory`)
        - Creating new middleware (e.g., rate limiting, additional content safety checks)
      - `fix`: **BUG RESOLUTION** - Correcting existing functionality that was not working as intended. Examples:
        - Fixing incorrect business logic in TaskItem (e.g., status transition validation)
        - Resolving AI agent function tool errors (e.g., incorrect emoji formatting)
        - Correcting data validation issues (e.g., allowing invalid priority values)
        - Fixing content safety false positives or detection issues
        - Correcting thread management bugs in TaskAgentService
      - `refactor`: **CODE IMPROVEMENT WITHOUT BEHAVIOR CHANGE** - Restructuring existing code without changing its external behavior or adding new features. Examples:
        - Extracting methods or classes for better organization (e.g., creating `AgentInstructionsBuilder`)
        - Renaming variables/methods for clarity (e.g., `_dict` to `_conversationThreads`)
        - Simplifying complex logic in TaskFunctions while maintaining same functionality
        - Moving code between projects for better Clean Architecture adherence
        - Applying design patterns (Repository, Factory, etc.)
        - Converting synchronous code to asynchronous without adding features
        - Removing unused code or dependencies
      - `perf`: **PERFORMANCE OPTIMIZATION** - Code changes that specifically improve performance without adding new features. Examples:
        - Adding database indexes
        - Optimizing queries (Entity Framework)
        - Implementing caching mechanisms
        - Reducing memory allocations
        - Improving algorithm efficiency
      - `build`: Changes that affect the build system or external dependencies (e.g., NuGet packages, MSBuild, Docker).
      - `ci`: Changes to CI/CD configuration files and scripts (e.g., Azure Pipelines, GitHub Actions, YML files).
      - `docs`: **DOCUMENTATION ONLY** - Changes exclusively to documentation files. Examples:
        - Modifying Markdown (.md) files (README.md, CONTENT_SAFETY.md, etc.)
        - Updating inline code comments or XML documentation
        - Updating API documentation or code examples in docs
        - **CRITICAL**: If a .md file is modified, the type MUST be `docs`, NOT `refactor(docs)` or any other type
      - `style`: Changes that do not affect the meaning of the code (white-space, formatting, missing semicolons, etc.).
      - `test`: Adding missing tests or correcting existing tests.
      - `revert`: Reverts a previous commit (use footer to reference reverted commits).
      - `chore`: Miscellaneous commits. Other changes that don't modify `src` or test files (e.g. .gitignore, package updates)
    - "**IMPORTANT**: If you're restructuring code without adding new functionality or changing behavior, use `refactor`, NOT `feat`."
    - "**CRITICAL FOR DOCUMENTATION**: If modifying ONLY .md files or documentation, use `docs` type, NEVER `refactor(docs)` or other types."
    - "If none of the types apply, use 'chore'."
  - **Scope (Optional but Strongly Recommended for this project):**
    - "**STRONGLY RECOMMENDED to include a scope** to provide context about what part of the codebase was affected."
    - "Scope is OPTIONAL per Conventional Commits spec, but provides valuable context."
    - "Use the following scope hierarchy (most specific applicable level):"
      - **Project Level**: `Domain`, `Application`, `Infrastructure`, `WebApp`
      - **Layer/Folder Level**: `Application.Functions`, `Application.DTOs`, `Infrastructure.Services`, `WebApp.Middleware`
      - **Feature/Component Level**: `ai-agent`, `task-management`, `content-safety`, `database`
    - "**Scope Examples for TaskAgent Project:**"
      - `feat(Application.Functions): add search tasks function tool`
      - `fix(Domain): correct task status transition validation`
      - `refactor(Infrastructure): extract agent instructions to separate class`
      - `perf(Infrastructure): optimize task retrieval query with AsNoTracking`
      - `test(Application): add task functions unit tests`
      - `docs(WebApp): update API endpoint documentation`
    - "If the change affects multiple projects/scopes, use the most general applicable scope or omit parentheses for cross-cutting changes."
  - **Description:**
    - "A concise description of the change in imperative, present tense (e.g., 'fix: correct typos in documentation', not 'fixed typos...')."
    - "Capitalize the first letter of the description."
    - "Do not end the description with a period."
    - "Limit the description to 50 characters."
- **Body (Optional but Recommended for Complex Changes):**
  - "**WHEN TO INCLUDE BODY:**"
    - "**REQUIRED when modifying multiple files** unless the title is self-explanatory"
    - "When the change needs explanation beyond the title"
    - "When explaining WHY the change was made (motivation)"
    - "When describing the impact or side effects"
    - "For complex refactoring or architectural changes"
  - "**WHEN BODY CAN BE OMITTED:**"
    - "Simple, self-explanatory single-file changes"
    - "The title completely describes the change"
    - "Trivial fixes or updates (e.g., 'docs: fix typo in README')"
  - "**BODY FORMAT AND STYLE:**"
    - "Write in **past tense** (describe what was done, not what to do)"
    - "Examples: 'Added feature X', 'Implemented Y', 'Fixed Z', 'Updated configuration'"
    - "Use complete sentences with proper capitalization and punctuation"
    - "Wrap lines at 72 characters for readability"
    - "Separate paragraphs with blank lines for better structure"
  - "**LISTING MODIFIED FILES:**"
    - "For **2-10 files**: List files with brief description of changes"
    - "Format: 'Modified files (X):' or 'Affected files (X):' followed by bulleted list"
    - "Use `-` (hyphen) for bullet points, not `•`"
    - "Example format: `- FileName.cs: Brief description of change`"
    - "File descriptions should use past tense (e.g., 'Added method', 'Updated logic')"
    - "For **>10 files**: Group by layer/component instead of listing all files"
    - "Example: 'This change spans multiple layers (15 files modified):'"
    - "Then list high-level changes by component/layer, not individual files"
    - "For **1 file**: Generally omit file listing (it's in the commit diff)"
  - "Explain the motivation for the change and how it differs from previous behavior."
  - "For significant changes, include performance metrics, testing notes, or migration guidance."
- **Footer (Optional):**
  - "Use the footer to reference issue trackers, breaking changes, or other metadata."
  - "**Breaking Changes:** Start with `BREAKING CHANGE: ` (or `BREAKING-CHANGE:`) followed by a description. Alternatively, append `!` after type/scope (e.g., `feat!:` or `feat(api)!:`)."
  - "**Issue References:** Use `Closes #issueNumber`, `Fixes #issueNumber`, `Resolves #issueNumber` to link to issues."
  - "**Other Footers:** May include `Reviewed-by:`, `Refs:`, `Acked-by:`, etc. following git trailer format."

**II.A. Breaking Changes - Detailed Guidelines:**

**When to Mark as BREAKING CHANGE (correlates with MAJOR version bump):**

A breaking change is ANY modification that requires consumers of your code to make changes to their codebase. Use `BREAKING CHANGE:` footer or `!` suffix when:

**1. API Contract Changes:**

- **Changing method signatures:**
  - Removing parameters from public methods
  - Changing parameter types or order
  - Changing return types
  - Example: `UpdateTask(int id, string title)` → `UpdateTask(int id, TaskUpdateDto dto)`
- **Removing or renaming public APIs:**
  - Deleting public methods, properties, or classes
  - Renaming interfaces (e.g., `ITaskService` → `ITaskAgentService`)
  - Removing endpoints (e.g., deleting `GET /api/tasks/legacy`)
- **Changing HTTP response structures:**
  - Removing fields from JSON responses
  - Changing field types (e.g., `"id": 123` → `"id": "abc-123"`)
  - Renaming response properties (e.g., `taskName` → `title`)
  - Example: `{ "id": 1, "name": "Task" }` → `{ "id": 1, "title": "Task", "metadata": {} }`

**2. Behavior Changes:**

- **Changing default behavior:**
  - Modifying default values that affect functionality
  - Changing validation rules that reject previously valid input
  - Altering error handling that changes response codes
  - Example: Changing default task status from "Pending" to "Draft"
- **Changing business logic:**
  - Modifying core algorithms that produce different results
  - Changing authorization/authentication requirements
  - Altering data persistence behavior (e.g., soft delete → hard delete)

**3. Data/Database Changes:**

- **Schema changes requiring migration:**
  - Removing database columns
  - Changing column types (e.g., `int` → `string`)
  - Adding non-nullable columns without defaults
  - Renaming tables or columns that break existing queries
- **Data format changes:**
  - Changing serialization format (JSON → XML)
  - Modifying date/time formats
  - Changing enum values or their numeric representations

**4. Configuration Changes:**

- **Removing or renaming configuration keys:**
  - `appsettings.json` key changes requiring user updates
  - Environment variable name changes
  - Example: `AzureOpenAI:ApiKey` → `Azure:OpenAI:Key`
- **Changing required configuration:**
  - Adding new required settings without defaults
  - Removing previously optional settings that become required

**5. Dependency Changes:**

- **Major framework version upgrades:**
  - .NET 8 → .NET 9 or .NET 9 → .NET 10
  - Entity Framework Core 8 → 9
  - Example: Upgrading to new SDK version with incompatible APIs
- **Removing dependencies:**
  - Removing packages that consumers might rely on
  - Changing database providers (SQL Server → PostgreSQL)

**6. Authentication/Authorization Changes:**

- **Changing security requirements:**
  - Adding authentication to previously public endpoints
  - Changing permission models or role requirements
  - Modifying token formats or validation rules

**When NOT to mark as BREAKING CHANGE:**

❌ **Pure refactoring (no external impact):**

- Internal code reorganization (moving files, renaming private methods)
- Extracting private helper classes
- Improving internal algorithms with same output
- Example: `refactor(Infrastructure): extract agent instructions to builder class` (internal change only)

❌ **Backward-compatible additions:**

- Adding new optional parameters with defaults
- Adding new methods/endpoints without removing existing ones
- Adding new properties to responses (additive only)
- Example: `feat(api): add optional filter parameter to GET /api/tasks` (old code still works)

❌ **Bug fixes that restore intended behavior:**

- Fixing incorrect validation that was a bug
- Correcting calculation errors
- Example: `fix(Domain): correct task priority validation logic` (fixing broken behavior)

❌ **Internal dependency updates (no API changes):**

- Updating packages that don't affect public API
- Minor/patch version bumps of dependencies
- Example: `build: update Newtonsoft.Json from 13.0.1 to 13.0.3`

❌ **Performance improvements (same behavior):**

- Optimizing queries or algorithms
- Adding caching or indexes
- Example: `perf(Infrastructure): add database indexes for task queries` (faster, but same results)

**Breaking Change Examples for TaskAgent Project:**

```
feat(api)!: change task response format to include metadata

BREAKING CHANGE: Task API responses now include a metadata object.
The response structure has changed from:
{
  "id": 1,
  "title": "Task",
  "status": "Pending"
}

To:
{
  "id": 1,
  "title": "Task",
  "status": "Pending",
  "metadata": {
    "createdBy": "user@example.com",
    "version": 1
  }
}

Clients must update their response parsing to handle the new structure.
```

```
refactor!: rename ITaskService to ITaskAgentService

BREAKING CHANGE: The interface ITaskService has been renamed to
ITaskAgentService for better clarity. All implementations and
consumers must update their references.

Migration:
- Update dependency injection registrations
- Change interface references in constructors
- Update mock objects in tests
```

```
build!: upgrade to .NET 10

BREAKING CHANGE: Project now targets .NET 10. Consumers must:
- Install .NET 10 SDK
- Update project target framework to net10.0
- Review breaking changes in .NET 10 release notes

This change enables use of new C# 13 features and improved
performance characteristics.
```

```
feat(Application.Functions)!: remove deprecated GetTaskByName function

BREAKING CHANGE: Removed the deprecated GetTaskByName function tool.
Use GetTaskDetails with ID parameter instead.

Migration path:
- Replace calls to GetTaskByName("task name")
- With GetTaskDetails(taskId) after looking up ID
```

```
fix(api)!: correct task status enum values

BREAKING CHANGE: Task status enum values have been corrected to
match business requirements. Status codes have changed:
- 0: Pending → Draft (was Pending)
- 1: InProgress (unchanged)
- 2: Completed (unchanged)
- 3: Cancelled (new)

Clients storing numeric status values must update their mappings.

Fixes #156
```

**Decision Tree for Breaking Changes:**

1. **Does it remove or rename public APIs?** → BREAKING CHANGE
2. **Does it change HTTP request/response structures?** → BREAKING CHANGE
3. **Does it require consumers to modify their code?** → BREAKING CHANGE
4. **Does it change database schema in a non-compatible way?** → BREAKING CHANGE
5. **Does it change configuration keys or requirements?** → BREAKING CHANGE
6. **Does it upgrade major framework versions (e.g., .NET 9 → 10)?** → BREAKING CHANGE
7. **Does it change authentication/authorization requirements?** → BREAKING CHANGE
8. **Is it an internal refactor with no external impact?** → NOT breaking
9. **Does it add optional features without removing existing ones?** → NOT breaking
10. **Does it fix a bug to restore intended behavior?** → NOT breaking (usually)

**III. Commit Message Examples:**

**Basic Examples (Single File Changes):**

- `feat(Application.Functions): add delete task function tool`
- `fix(Domain): correct completed to pending transition validation`
- `refactor(Infrastructure): extract AI agent instructions builder`
- `perf(Infrastructure): optimize task queries with AsNoTracking`
- `test(Application): add task functions unit tests`
- `docs: update content safety documentation`
- `docs(README): add setup instructions for Azure OpenAI`

**Examples with Breaking Changes:**

- `feat(api)!: change task response format to include metadata`
- `refactor!: rename ITaskService to ITaskAgentService`

**Detailed Examples with Body (Multi-file or Complex Changes):**

```
feat(WebApp.Middleware): add prompt injection detection

Implemented Azure Content Safety prompt shield to detect
and block jailbreak attempts before reaching the AI agent.
Added 400 status response with descriptive error message.

Modified files (3):
- ContentSafetyMiddleware.cs: Added prompt shield check
- ContentSafetyService.cs: Implemented shield API call
- IContentSafetyService.cs: Added interface method

Closes #42
```

```
refactor(Domain): extract task validation to factory method

Moved validation logic from constructor to TaskItem.Create()
factory method following domain-driven design principles.
This ensures all task creation goes through proper validation.

Affected files (2):
- TaskItem.cs: Extracted Create() factory method
- TaskRepository.cs: Updated to use factory method

This change maintains the same validation behavior while
improving code organization and testability.
```

```
perf(Infrastructure): add database indexes for task queries

Added composite index on Status, Priority, and CreatedAt
columns to optimize filtering queries. Performance tests
showed 75% reduction in query time for GetAllTasksAsync
with multiple filters applied.

Modified files (2):
- TaskDbContext.cs: Added index configuration
- 20251018_AddTaskIndexes.cs: New EF migration

Refs: #89
```

```
feat(Application): implement task search and filtering

Added comprehensive search and filtering capabilities for
tasks including full-text search, status filtering, priority
filtering, and date range queries.

This change spans multiple layers (12 files modified):
- Application layer: New DTOs, interfaces, and service methods
- Infrastructure layer: Repository implementations and queries
- WebApp layer: New API endpoints and request validators

Key changes:
- Created SearchTasksDto with filter parameters
- Implemented full-text search on Title and Description
- Added composite indexes for performance
- Created GET /api/tasks/search endpoint
- Added pagination support (page size, page number)

Performance: Search queries execute in <100ms for 10k records.

Closes #45, #67, #89
```

```
docs: update architecture and setup documentation

Updated project documentation to reflect current architecture
patterns and setup requirements. Added comprehensive content
safety testing guide with 75+ test cases.

Modified files (4):
- README.md: Added Azure OpenAI setup section
- CONTENT_SAFETY.md: New file with testing guide
- .github/copilot-instructions.md: Updated DI patterns
- .github/git-commit-messages-instructions.md: Added examples
```

**Cross-Layer Examples:**

- `feat: add content safety middleware with 4-layer protection`
- `fix: resolve thread management in AI agent service`
- `refactor: standardize error handling across all layers`

**Handling Commits with Multiple Change Types:**

- "Each commit should be as atomic as possible, addressing a single concern. A single commit must only have one type."
- "If a commit includes multiple types of changes (e.g., a new feature and a refactor), choose the type that represents the primary purpose of the commit. The hierarchy is generally `feat` > `fix` > `perf` > `refactor`."
- "**Example**: If you add a new feature and also refactor some old code in the same file, the commit type must be `feat`."
  - `feat(Application.Functions): add update task priority tool` (even if it involved refactoring)
- "**AVOID** creating a single commit message that lists multiple types. A commit has ONE type."
- "**INCORRECT**: `feat: add function, refactor: simplify entity`"
- "**CORRECT**: `feat(Application.Functions): add search tasks by priority` (This is the main change, even if refactoring was done)

**IV. Instructions for Copilot:**

- "When generating commit messages, adhere strictly to the Conventional Commits specification ([https://www.conventionalcommits.org/en/v1.0.0/](https://www.conventionalcommits.org/en/v1.0.0/))."
- "**CRITICAL**: Distinguish carefully between `feat` and `refactor`:"
  - "Use `feat` ONLY when adding NEW functionality or capabilities"
  - "Use `refactor` when improving existing code structure without changing behavior"
  - "If restructuring code for better architecture = `refactor`"
  - "If adding new business logic or endpoints = `feat`"
- "**CRITICAL FOR DOCUMENTATION FILES**: When ONLY .md files are modified, use `docs` type, NEVER `refactor(docs)` or other combinations. Examples:"
  - "✅ CORRECT: `docs: update README with setup steps`"
  - "✅ CORRECT: `docs(README): add Azure configuration guide`"
  - "❌ INCORRECT: `refactor(docs): update README`"
  - "❌ INCORRECT: `chore(docs): update documentation`"
- "**STRONGLY RECOMMENDED to include a scope** using the project hierarchy: `ProjectName` or `ProjectName.FolderName`"
- "For TaskAgent Clean Architecture project, common scopes include:"
  - "`Domain`, `Application`, `Infrastructure`, `WebApp`"
  - "`Application.Functions`, `Application.DTOs`, `Infrastructure.Services`, `WebApp.Middleware`"
  - "`Domain.Entities`, `Infrastructure.Data`, `WebApp.Controllers`"
- "Use specific scopes like `ai-agent`, `task-management`, `content-safety`, `database` for feature-level changes"
- "For documentation changes, scope can reference the file name (e.g., `docs(README):`) or be omitted (e.g., `docs:`)"
- "Write descriptions in imperative, present tense with capital first letter"
- "Limit header to 50 characters, body lines to 72 characters"
- "**INCLUDE BODY when:**"
  - "Multiple files are modified (unless title is completely self-explanatory)"
  - "Complex changes that need explanation"
  - "Explaining WHY (motivation) not just WHAT"
  - "**ALWAYS include body when marking BREAKING CHANGE** to explain impact and migration path"
- "**OMIT BODY when:**"
  - "Single file, simple change"
  - "Title is completely self-explanatory"
  - "Trivial updates (e.g., 'docs: fix typo')"
- "**BODY WRITING STYLE - CRITICAL:**"
  - "**Header (title)**: Use IMPERATIVE PRESENT tense (e.g., 'add feature', 'fix bug', 'update config')"
  - "**Body**: Use PAST TENSE (e.g., 'Added feature', 'Fixed bug', 'Updated config')"
  - "Body describes what WAS done, header describes what the commit DOES"
  - "Use `-` (hyphen) for bullet points in body, NEVER `•` or other symbols"
  - "File listings format:"
    - "2-10 files: List each file with hyphen. Example: `- FileName.cs: Added method`"
    - ">10 files: Group by layer/component, don't list all files individually"
  - "Example for many files:"
    - "❌ DON'T: List all 15 files individually (too verbose)"
    - "✅ DO: 'This change spans multiple layers (15 files modified):'"
    - "Then describe by component: 'Application layer: New DTOs and services'"
  - "Wrap lines at 72 characters for readability"
- "**BREAKING CHANGES - Critical Decision Guide:**"
  - "Use `BREAKING CHANGE:` footer or `!` suffix when consumers MUST modify their code"
  - "**Ask yourself**: Would existing code that uses this API/endpoint/interface still work?"
  - "**If NO** (requires consumer changes) → BREAKING CHANGE"
  - "**If YES** (backward compatible) → NOT breaking"
  - "Common breaking changes:"
    - "Removing/renaming public APIs, methods, properties, endpoints"
    - "Changing HTTP response structures (removing/renaming fields)"
    - "Changing method signatures (parameters, return types)"
    - "Major framework upgrades (.NET 9 → .NET 10)"
    - "Removing/renaming configuration keys in appsettings.json"
    - "Changing database schema (removing columns, changing types)"
    - "Changing authentication/authorization requirements"
  - "NOT breaking changes:"
    - "Internal refactoring (private methods, file organization)"
    - "Adding optional parameters with defaults"
    - "Adding new methods/endpoints (keeping existing ones)"
    - "Performance improvements (same behavior/output)"
    - "Bug fixes restoring intended behavior"
    - "Minor dependency updates without API changes"
  - "**Format**: Use `type(scope)!: description` OR add footer `BREAKING CHANGE: explanation`"
  - "**Body REQUIRED**: Explain what breaks, why, and how to migrate"
- "Use footer for breaking changes (`BREAKING CHANGE: ` or `!` in header) and issue references (`Closes #123`)"
- "When in doubt between types, prefer the more specific type (e.g., `perf` over `refactor` for performance improvements)"

**V. TaskAgent Project Specific Guidelines:**

**Common Scenarios and Correct Types:**

1. **Adding new functionality (`feat`):**

   - New AI agent function tools (TaskFunctions methods)
   - New API endpoints or controllers (ChatController, TaskController)
   - New business logic or domain entities (TaskItem methods, new entities)
   - New content safety features or middleware layers
   - New validation rules or business rules in Domain
   - New middleware components (rate limiting, logging)
   - New Entity Framework migrations with new features

2. **Code improvements without new features (`refactor`):**

   - Extracting AI agent instructions to separate builder class
   - Moving code between Clean Architecture layers
   - Applying design patterns (Repository, Factory, Strategy)
   - Converting synchronous to asynchronous without adding functionality
   - Simplifying TaskFunctions error handling while maintaining same behavior
   - Reorganizing folder structure (DTOs, Functions, Services)
   - Renaming for clarity (e.g., `_threads` to `_conversationThreads`)
   - Consolidating duplicate code in function tools

3. **Performance improvements (`perf`):**

   - Database query optimizations with AsNoTracking()
   - Adding indexes on TaskItem (Status, Priority, CreatedAt)
   - Implementing caching for AI agent responses
   - Optimizing TaskDbContext queries
   - Reducing memory allocations in TaskAgentService
   - Optimizing thread dictionary management
   - Improving content safety API call efficiency

4. **Bug fixes (`fix`):**
   - Correcting TaskItem business logic (status transitions)
   - Fixing AI agent function tool errors or incorrect responses
   - Resolving content safety false positives
   - Fixing task validation problems (title length, priority values)
   - Correcting DI configuration issues in Program.cs
   - Fixing null reference exceptions in TaskFunctions
   - Resolving Entity Framework enum conversion issues

**Scope Naming Conventions for this Project:**

- **Project Level**: `Domain`, `Application`, `Infrastructure`, `WebApp`
- **Layer/Folder Level**: `Application.Functions`, `Application.DTOs`, `Application.Interfaces`, `Infrastructure.Data`, `Infrastructure.Services`, `WebApp.Middleware`, `WebApp.Controllers`
- **Feature Level**: `ai-agent`, `task-management`, `content-safety`, `database`, `chat`
- **Component Level**: `middleware`, `controllers`, `repositories`, `function-tools`, `entities`

**Decision Tree for Commit Types:**

1. **Does it modify ONLY .md files or documentation?** → `docs` (NEVER `refactor(docs)`)
2. **Does it add new user-facing functionality?** → `feat`
3. **Does it fix broken functionality?** → `fix`
4. **Does it improve performance measurably?** → `perf`
5. **Does it change code structure without changing behavior?** → `refactor`
6. **Does it add/modify tests only?** → `test`
7. **Does it change CI/CD configuration?** → `ci`
8. **Does it change build system or dependencies?** → `build`
9. **Does it revert a previous commit?** → `revert`
10. **Everything else** → `chore`

**Multi-File Changes - Body Guidelines:**

- **Include body if:**
  - 2+ files modified (describe changes and list files if ≤10)
  - Change spans multiple layers or scopes
  - Explanation needed for WHY (not just WHAT)
  - Breaking changes or migrations involved
- **Body can be omitted if:**
  - Single file, straightforward change
  - Title like "docs: fix typo in README" is self-explanatory
  - Trivial formatting or style changes

**File Listing Best Practices:**

- **2-10 files modified:**
  - Include section: "Modified files (X):" or "Affected files (X):"
  - List each file with hyphen and brief description
  - Example: `- TaskService.cs: Added search method`
  - Use past tense for descriptions
- **>10 files modified:**
  - DON'T list all files individually (commit becomes too long)
  - State total count: "This change spans multiple layers (15 files modified):"
  - Group changes by layer/component/category
  - Example: "Application layer: New DTOs, interfaces, and service methods"
  - Example: "Infrastructure layer: Repository implementations and queries"
  - Focus on WHAT changed in each component, not listing every file
- **1 file modified:**
  - Generally omit file listing (obvious from commit diff)
  - Use body to explain WHY and HOW, not WHAT file

**Body Writing Style:**

- Header (title): Imperative present ("add feature", "fix bug")
- Body: Past tense ("Added feature", "Implemented logic", "Fixed validation")
- Bullet points: Use `-` (hyphen), not `•` or other symbols
- Line length: Wrap at 72 characters
- Paragraphs: Separate with blank lines for readability
