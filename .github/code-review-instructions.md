# GitHub Copilot Code Review Instructions

## Purpose

These instructions guide code reviewers to ensure high-quality, maintainable, and consistent code that follows industry best practices and project standards.

## Using Microsoft Learn MCP for Reviews

**Leverage Microsoft Learn MCP to validate technical decisions:**

- Verify latest .NET and C# best practices
- Check Azure service recommendations and patterns
- Validate ASP.NET Core implementations
- Confirm Entity Framework Core usage patterns
- Reference official Microsoft documentation

**When reviewing, use MCP to:**

- Validate that code follows current Microsoft guidelines
- Confirm API usage is correct and up-to-date
- Find official examples for comparison
- Check for deprecated methods or patterns

---

## Review Checklist

### 🏗️ Architecture & Design

#### Clean Architecture Compliance

- [ ] Dependencies flow inward (UI → Application → Domain)
- [ ] Domain layer has NO external dependencies
- [ ] Application layer defines interfaces, Infrastructure implements them
- [ ] Business logic resides in Domain entities, not in controllers or services

#### SOLID Principles

- [ ] **SRP**: Each class has a single, well-defined responsibility
- [ ] **OCP**: Code is extensible without modification (uses interfaces/abstractions)
- [ ] **LSP**: Derived classes can substitute base classes without breaking functionality
- [ ] **ISP**: Interfaces are focused and specific, not bloated
- [ ] **DIP**: High-level modules depend on abstractions, not concrete implementations

#### Design Principles

- [ ] **DRY**: No duplicated code or logic
- [ ] **KISS**: Solutions are simple and straightforward, not over-engineered
- [ ] **YAGNI**: Only implements what's currently needed, no speculative features
- [ ] **LOD**: Methods don't chain multiple calls (avoid `obj.GetA().GetB().GetC()`)

---

### 📁 Code Organization

#### File Structure

- [ ] One class per file (except related DTOs)
- [ ] No nested classes (each class in its own file)
- [ ] Files are in correct layer folders (Domain, Application, Infrastructure, WebApp)
- [ ] Naming conventions followed (PascalCase for classes, camelCase for variables)

#### Constants & Configuration

- [ ] No magic numbers or strings
- [ ] Constants defined in appropriate layer:
  - Domain: Business rules, validation limits
  - Application: Service configuration
  - Infrastructure: Connection strings, cache keys
  - WebApp: API routes, HTTP headers
- [ ] Constants are well-named and grouped logically

#### Folder Organization

- [ ] Files are in the correct folders based on their purpose
- [ ] Folder structure follows Clean Architecture layers
- [ ] Related files are grouped together logically
- [ ] No generic folder names like "Helpers" or "Utilities"

---

### 💻 Code Quality

#### Naming & Readability

- [ ] Names are meaningful, pronounceable, and searchable
- [ ] PascalCase for classes, methods, properties
- [ ] camelCase for parameters and local variables
- [ ] UPPER_CASE for constants
- [ ] Boolean variables start with "is", "has", "can", etc.
- [ ] Method names clearly describe what they do (verbs)

#### Functions & Methods

- [ ] Functions are small and do one thing well (< 20 lines ideally)
- [ ] Methods have clear, single responsibilities
- [ ] Complex logic is broken down into smaller, named methods
- [ ] Parameters are limited (ideally ≤ 3, max 5)
- [ ] Async methods have "Async" suffix

#### Control Flow

- [ ] Uses guard clauses (early returns) instead of deep nesting
- [ ] Avoids nested if-statements (max 2-3 levels)
- [ ] Switch expressions or pattern matching used where appropriate
- [ ] Loops are simple and have clear exit conditions

#### Comments & Documentation

- [ ] Code is self-documenting (clear names, simple logic)
- [ ] XML comments on public APIs (classes, methods, properties)
- [ ] Comments explain "why", not "what" (code shows "what")
- [ ] No commented-out code (use version control)
- [ ] Complex algorithms have explanatory comments

---

### 🔒 Error Handling & Validation

#### Exception Handling

- [ ] Exceptions used for exceptional cases, not control flow
- [ ] Specific exception types used (not generic `Exception`)
- [ ] Exceptions provide meaningful messages
- [ ] Resources cleaned up properly (using/try-finally)
- [ ] No swallowed exceptions (empty catch blocks)

#### Input Validation

- [ ] All public methods validate parameters
- [ ] Validation occurs at appropriate layer (Domain entities, DTOs)
- [ ] Guard clauses check for null, empty, or invalid inputs
- [ ] Business rule validation in Domain entities
- [ ] Input validation in DTOs or validators (FluentValidation)

---

### 🎯 Domain-Driven Design (DDD)

#### Entities & Value Objects

- [ ] Entities created through factory methods, not constructors
- [ ] Business rules encapsulated in entity methods
- [ ] Private setters protect entity invariants
- [ ] Value objects are immutable
- [ ] Entity state changes through methods, not property setters

#### Business Logic

- [ ] Business logic in Domain layer, not in services or controllers
- [ ] Domain entities enforce business rules
- [ ] Domain events used for side effects (if applicable)
- [ ] Rich domain models, not anemic models with only getters/setters

---

### 🗄️ Data Access & Persistence

#### Repository Pattern

- [ ] Repositories implement interfaces defined in Application layer
- [ ] Repository methods are specific and focused
- [ ] `.AsNoTracking()` used for read-only queries
- [ ] Tracking enabled for updates
- [ ] Async methods used throughout
- [ ] No business logic in repositories

#### Entity Framework

- [ ] DbContext configured properly
- [ ] Entity configurations in separate files
- [ ] Migrations have descriptive names
- [ ] No raw SQL (use LINQ), unless necessary for performance
- [ ] Proper loading strategies (eager/lazy/explicit)

---

### ⚡ Performance & Best Practices

#### Async/Await

- [ ] Async methods throughout the call stack
- [ ] `ConfigureAwait(false)` in library code
- [ ] No `async void` (except event handlers)
- [ ] No blocking calls (`.Result`, `.Wait()`)
- [ ] Async suffix on method names

#### Memory & Resources

- [ ] `IDisposable` implemented where needed
- [ ] `using` statements for disposable resources
- [ ] Large collections use streaming/pagination
- [ ] No unnecessary allocations in loops
- [ ] StringBuilder for string concatenation in loops

#### LINQ & Collections

- [ ] Appropriate collection types (List, HashSet, Dictionary)
- [ ] LINQ queries are efficient
- [ ] No multiple enumerations of IEnumerable
- [ ] Deferred execution understood and used correctly

---

### 🧪 Testing Considerations

#### Testability

- [ ] Code is testable (dependencies injected, not instantiated)
- [ ] Methods have clear inputs and outputs
- [ ] Side effects are minimized
- [ ] Dependencies are mockable (interfaces)
- [ ] No static methods (unless pure functions)

#### Test Coverage

- [ ] Unit tests exist for business logic
- [ ] Integration tests for repositories/data access
- [ ] Tests are clear and descriptive
- [ ] Tests follow Arrange-Act-Assert pattern
- [ ] Edge cases and error conditions tested

---

### 🔐 Security & Safety

#### Security Practices

- [ ] No hardcoded secrets or credentials
- [ ] Sensitive data properly handled
- [ ] SQL injection prevented (parameterized queries/LINQ)
- [ ] XSS prevention (encoded output)
- [ ] Authentication/authorization properly implemented
- [ ] Input sanitized and validated

#### Null Safety

- [ ] Nullable reference types enabled (C# 8+)
- [ ] Null checks where appropriate
- [ ] Null-forgiving operator used sparingly and justified
- [ ] Default values or null object pattern where appropriate

---

### 📝 Specific Code Patterns to Look For

#### ❌ Anti-Patterns to Reject

**God Objects / Classes**

```csharp
// ❌ BAD - Too many responsibilities
public class TaskManager
{
    public void Create() { }
    public void Update() { }
    public void Delete() { }
    public void SendEmail() { }
    public void LogActivity() { }
    public void ExportToPdf() { }
    public void GenerateReport() { }
}
```

**Nested Classes**

```csharp
// ❌ BAD - Nested classes
public class TaskService
{
    private class TaskValidator { }  // Should be separate file
}
```

**Magic Numbers/Strings**

```csharp
// ❌ BAD
if (task.Priority > 5) { }
if (status == "completed") { }
```

**Deep Nesting**

```csharp
// ❌ BAD
if (x) {
    if (y) {
        if (z) {
            if (w) { }
        }
    }
}
```

**Exception for Control Flow**

```csharp
// ❌ BAD
try {
    var item = GetItem(id);
} catch (NotFoundException) {
    return null;  // Expected case
}
```

#### ✅ Good Patterns to Approve

**Factory Methods**

```csharp
// ✅ GOOD
public class TaskItem
{
    private TaskItem() { }

    public static TaskItem Create(string title, string description)
    {
        // Validation
        return new TaskItem { Title = title };
    }
}
```

**Guard Clauses**

```csharp
// ✅ GOOD
public async Task<TaskItem> ProcessAsync(int id, string userId)
{
    if (id <= 0) return null;
    if (string.IsNullOrEmpty(userId)) return null;

    var task = await _repository.GetByIdAsync(id);
    if (task == null) return null;

    return task;
}
```

**Dependency Injection**

```csharp
// ✅ GOOD
public class TaskService
{
    private readonly ITaskRepository _repository;
    private readonly ILogger<TaskService> _logger;

    public TaskService(ITaskRepository repository, ILogger<TaskService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
}
```

**Constants Usage**

```csharp
// ✅ GOOD
public static class TaskConstants
{
    public const int MAX_TITLE_LENGTH = 200;
}

if (title.Length > TaskConstants.MAX_TITLE_LENGTH) { }
```

---

## Review Process

### 1️⃣ First Pass: High-Level Review (5-10 minutes)

- [ ] Review PR description and linked work items
- [ ] Check overall architecture and design decisions
- [ ] Verify files are in correct locations
- [ ] Identify major concerns or red flags

### 2️⃣ Second Pass: Detailed Review (15-30 minutes)

- [ ] Review each file line by line
- [ ] Check logic correctness and edge cases
- [ ] Verify error handling
- [ ] Look for code smells and anti-patterns
- [ ] Consider performance implications
- [ ] Check test coverage

### 3️⃣ Third Pass: Polish & Best Practices (5-10 minutes)

- [ ] Review naming conventions
- [ ] Check code documentation
- [ ] Verify consistency with project standards
- [ ] Suggest improvements and optimizations
- [ ] Verify no debug code or commented code

---

## Providing Feedback

### 🎯 Effective Feedback Guidelines

**Be Constructive**

- Focus on the code, not the person
- Explain "why" something should change
- Suggest alternatives, don't just criticize
- Acknowledge good implementations

**Be Specific**

- Point to exact lines or patterns
- Provide code examples when possible
- Reference documentation or standards
- Explain the impact of issues found

**Be Timely**

- Review PRs within 24 hours when possible
- Don't wait to batch all comments
- Respond to author questions promptly

**Categorize Comments**

- **Critical**: Must fix before merge (bugs, security issues)
- **Important**: Should fix before merge (maintainability, performance)
- **Suggestion**: Nice to have (improvements, optimizations)
- **Question**: Seeking clarification or understanding
- **Praise**: Acknowledge good work

### 💬 Comment Examples

**❌ Poor Feedback**

```
This is wrong.
Bad code.
Why did you do it this way?
```

**✅ Good Feedback**

```
Critical: This can cause a NullReferenceException when task is null.
Add a null check before accessing task.Owner.

Suggestion: Consider extracting this validation logic into a separate
method to follow the Single Responsibility Principle. Example:
private bool ValidateTask(TaskItem task) { ... }

Question: What's the reason for using List<T> here instead of
IEnumerable<T>? If we don't need indexed access, IEnumerable would
be more flexible.

Praise: Great use of guard clauses here! Makes the code much more
readable.
```

---

## When to Approve vs. Request Changes

### ✅ Approve When:

- All critical issues are resolved
- Code follows project standards
- Tests pass and coverage is adequate
- No security vulnerabilities
- Performance is acceptable
- Documentation is complete

### 🔄 Request Changes When:

- Critical bugs or security issues exist
- Violates core principles (SOLID, Clean Architecture)
- Missing essential tests
- Poor error handling
- Significant performance concerns
- Unclear or undocumented complex logic

### 💬 Comment (No Block) When:

- Minor style issues
- Suggestions for improvements
- Questions for clarification
- Nice-to-have optimizations
- Praise for good implementations

---

## Review Time Guidelines

- **Small PRs** (< 100 lines): 10-15 minutes
- **Medium PRs** (100-400 lines): 20-30 minutes
- **Large PRs** (400+ lines): Split into smaller PRs or dedicate 45-60 minutes

**Note**: PRs should ideally be < 400 lines for effective review.

---

## Final Checklist Before Approval

- [ ] All critical comments addressed
- [ ] Build passes successfully
- [ ] All tests pass
- [ ] Code coverage meets requirements
- [ ] No merge conflicts
- [ ] Branch is up-to-date with target branch
- [ ] PR description is clear and complete
- [ ] Linked work items are correct
- [ ] No debug code or TODO comments (unless documented)
- [ ] Follows all project coding standards

---

_These review guidelines ensure code quality, maintainability, and consistency across the project while promoting best practices and continuous improvement._
