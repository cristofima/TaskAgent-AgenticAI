# GitHub Copilot Custom Instructions - Git Conventional Commit Messages

These instructions guide GitHub Copilot in generating Git commit messages that adhere to the Conventional Commits specification.

**I. Conventional Commits Specification:**

- "Generate commit messages that follow the Conventional Commits specification ([https://conventionalcommits.org/](https://conventionalcommits.org/))."
- "Structure commit messages with a type, an optional scope, and a description: `type(scope)?: description`."
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
      - `docs`: Documentation only changes such as in Markdown (.md) files, comments for APIs or code in general.
      - `style`: Changes that do not affect the meaning of the code (white-space, formatting, missing semicolons, etc.).
      - `test`: Adding missing tests or correcting existing tests.
      - `op`: Changes that affect operational components like infrastructure, deployment, backup, recovery, etc.
      - `chore`: Miscellaneous commits. Other changes that don't modify `src` or test files (e.g. .gitignore)
    - "**IMPORTANT**: If you're restructuring code without adding new functionality or changing behavior, use `refactor`, NOT `feat`."
    - "If none of the types apply, use 'chore'."
  - **Scope (Required for this project):**
    - "**ALWAYS include a scope** to provide context about what part of the codebase was affected."
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
- **Body (Optional):**
  - "Include a longer description of the changes, if necessary. Use complete sentences."
  - "Explain the motivation for the change."
  - "Wrap lines at 72 characters."
- **Footer (Optional):**
  - "Use the footer to reference issue trackers or breaking changes."
  - "**Breaking Changes:** Start with `BREAKING CHANGE: ` followed by a description of the breaking change."
  - "**Issue References:** Use `Closes #issueNumber`, `Fixes #issueNumber` or `Resolves #issueNumber` to link to issues."

**III. Commit Message Examples:**

**Basic Examples:**

- `feat(Application.Functions): add delete task function tool`
- `fix(Domain): correct completed to pending transition validation`
- `refactor(Infrastructure): extract AI agent instructions builder`
- `perf(Infrastructure): optimize task queries with AsNoTracking`
- `test(Application): add task functions unit tests`
- `docs(WebApp): update chat API documentation`

**Detailed Examples with Body:**

```
feat(WebApp.Middleware): add prompt injection detection

Implement Azure Content Safety prompt shield to detect
and block jailbreak attempts before reaching the AI agent.
Returns 400 status with descriptive error message.

Closes #42
```

```
refactor(Domain): extract task validation to factory method

Move validation logic from constructor to TaskItem.Create()
factory method following domain-driven design principles.
Ensures all task creation goes through proper validation.
```

```
perf(Infrastructure): add database indexes for task queries

Add composite index on Status, Priority, and CreatedAt columns
to optimize filtering queries. Reduces query time by 75% for
GetAllTasksAsync with multiple filters.
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

- "When generating commit messages, adhere strictly to the Conventional Commits specification."
- "**CRITICAL**: Distinguish carefully between `feat` and `refactor`:"
  - "Use `feat` ONLY when adding NEW functionality or capabilities"
  - "Use `refactor` when improving existing code structure without changing behavior"
  - "If restructuring code for better architecture = `refactor`"
  - "If adding new business logic or endpoints = `feat`"
- "**ALWAYS include a scope** using the project hierarchy: `ProjectName` or `ProjectName.FolderName`"
- "For TaskAgent Clean Architecture project, common scopes include:"
  - "`Domain`, `Application`, `Infrastructure`, `WebApp`"
  - "`Application.Functions`, `Application.DTOs`, `Infrastructure.Services`, `WebApp.Middleware`"
  - "`Domain.Entities`, `Infrastructure.Data`, `WebApp.Controllers`"
- "Use specific scopes like `ai-agent`, `task-management`, `content-safety`, `database` for feature-level changes"
- "Write descriptions in imperative, present tense with capital first letter"
- "Limit header to 50 characters, body lines to 72 characters"
- "Add body for complex changes explaining motivation and context"
- "Use footer for breaking changes (`BREAKING CHANGE: `) and issue references (`Closes #123`)"
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

1. **Does it add new user-facing functionality?** → `feat`
2. **Does it fix broken functionality?** → `fix`
3. **Does it improve performance measurably?** → `perf`
4. **Does it change code structure without changing behavior?** → `refactor`
5. **Does it add/modify tests only?** → `test`
6. **Does it change documentation only?** → `docs`
7. **Everything else** → `chore`
