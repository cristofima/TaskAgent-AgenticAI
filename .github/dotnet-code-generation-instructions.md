# GitHub Copilot Custom Instructions - C#/.NET Backend Code Generation

These instructions guide GitHub Copilot in generating clean, maintainable, scalable, and type-safe code for C#/.NET projects following industry best practices and SOLID principles.

---

## Important Note

**DO NOT create `.md` documentation files with every prompt unless explicitly requested.**

## Using Microsoft Learn MCP

**Always use the Microsoft Learn MCP (Model Context Protocol) to consult up-to-date information** about:

- .NET and C# best practices and latest features
- Azure services, SDKs, and APIs
- ASP.NET Core patterns and recommendations
- Entity Framework Core updates
- Microsoft official documentation and guidelines

**When to use Microsoft Learn MCP:**

- Before implementing new features or patterns
- When unsure about current best practices
- To verify API signatures and methods
- To find official Microsoft code samples
- To ensure compliance with latest Microsoft guidelines

**Available tools:**

- `microsoft_docs_search` - Search official Microsoft documentation
- `microsoft_code_sample_search` - Find official code examples
- `microsoft_docs_fetch` - Get complete documentation pages

**Example usage:**

```
Use Microsoft Learn MCP to find the latest best practices for:
- Implementing dependency injection in ASP.NET Core
- Using Azure OpenAI SDK
- Configuring Entity Framework Core
- Implementing middleware in ASP.NET Core
```

## File Organization

### One Class Per File Rule

**Only one class should exist per file**, unless dealing with closely related DTOs that serve the same domain concept.

**Important**: This rule also means **NO nested classes** (classes within classes). Each class should have its own file for better maintainability and testability.

**Example - Correct:**

```
✅ ChatRequest.cs (contains only ChatRequest DTO)
✅ ChatResponse.cs (contains only ChatResponse DTO)
✅ TaskItem.cs (contains only TaskItem entity)
```

**Example - Exception (Related DTOs):**

```
✅ TaskDTOs.cs (may contain TaskCreateDto, TaskUpdateDto, TaskResponseDto)
```

**Example - Incorrect (Nested Classes):**

```csharp
// ❌ BAD - Nested classes
public class TaskService
{
    private class TaskValidator  // Don't nest classes
    {
        public bool Validate(TaskItem task) { }
    }

    private class TaskLogger  // Don't nest classes
    {
        public void Log(string message) { }
    }
}

// ✅ GOOD - Separate files
// File: TaskService.cs
public class TaskService { }

// File: TaskValidator.cs
public class TaskValidator
{
    public bool Validate(TaskItem task) { }
}

// File: TaskLogger.cs
public class TaskLogger
{
    public void Log(string message) { }
}
```

### Avoid Magic Numbers and Strings

**Always use constants instead of magic numbers or strings**. Constants should be defined in the appropriate layer based on their scope and purpose.

**Where to place constants:**

1. **Domain Layer** (`TaskAgent.Domain/Constants/`): Business domain constants
   - Domain-specific limits, business rules values
   - Entity validation constants
2. **Application Layer** (`TaskAgent.Application/Constants/`): Application-specific constants
   - Service configuration values
   - Application settings constants
3. **Infrastructure Layer** (`TaskAgent.Infrastructure/Constants/`): Infrastructure constants
   - Connection string keys
   - Cache keys, external API endpoints
4. **WebApp Layer** (`TaskAgent.WebApp/Constants/`): Web-specific constants
   - Route paths, HTTP header names
   - UI-related constants

**Example:**

```csharp
// ❌ BAD - Magic numbers and strings
public class TaskItem
{
    public static TaskItem Create(string title, string description)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required");

        if (title.Length > 200)  // Magic number
            throw new ArgumentException("Title is too long");

        if (description.Length > 1000)  // Magic number
            throw new ArgumentException("Description is too long");

        return new TaskItem { Title = title };
    }
}

public class TaskController : ControllerBase
{
    [HttpGet("api/tasks")]  // Magic string
    public IActionResult GetTasks()
    {
        var tasks = _repository.GetAll();
        return Ok(tasks);
    }
}

// ✅ GOOD - Using constants
// File: TaskAgent.Domain/Constants/TaskConstants.cs
namespace TaskAgent.Domain.Constants
{
    public static class TaskConstants
    {
        public const int MAX_TITLE_LENGTH = 200;
        public const int MAX_DESCRIPTION_LENGTH = 1000;
        public const int MIN_PRIORITY_VALUE = 1;
        public const int MAX_PRIORITY_VALUE = 5;
    }

    public static class ValidationMessages
    {
        public const string TITLE_REQUIRED = "Title is required";
        public const string TITLE_TOO_LONG = "Title cannot exceed maximum length";
        public const string DESCRIPTION_TOO_LONG = "Description cannot exceed maximum length";
    }
}

// File: TaskAgent.Domain/Entities/TaskItem.cs
public class TaskItem
{
    public static TaskItem Create(string title, string description)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException(ValidationMessages.TITLE_REQUIRED);

        if (title.Length > TaskConstants.MAX_TITLE_LENGTH)
            throw new ArgumentException(ValidationMessages.TITLE_TOO_LONG);

        if (description.Length > TaskConstants.MAX_DESCRIPTION_LENGTH)
            throw new ArgumentException(ValidationMessages.DESCRIPTION_TOO_LONG);

        return new TaskItem { Title = title };
    }
}

// File: TaskAgent.WebApp/Constants/ApiRoutes.cs
namespace TaskAgent.WebApp.Constants
{
    public static class ApiRoutes
    {
        public const string BASE = "api";
        public const string TASKS = $"{BASE}/tasks";
        public const string CHAT = $"{BASE}/chat";
    }
}

// File: TaskAgent.WebApp/Controllers/TaskController.cs
public class TaskController : ControllerBase
{
    [HttpGet(ApiRoutes.TASKS)]
    public IActionResult GetTasks()
    {
        var tasks = _repository.GetAll();
        return Ok(tasks);
    }
}
```

### Clean Architecture Folder Structure

The project should follow a clear folder structure aligned with Clean Architecture layers. Each layer has specific responsibilities and folder organization.

#### Domain Layer (`TaskAgent.Domain`)

**Purpose**: Core business logic with NO external dependencies.

```
TaskAgent.Domain/
├── Constants/           # Domain business constants
│   ├── TaskConstants.cs
│   └── ValidationMessages.cs
├── Entities/           # Domain entities (business objects)
│   ├── TaskItem.cs
│   └── User.cs
├── Enums/             # Domain enumerations
│   ├── TaskStatus.cs
│   └── TaskPriority.cs
├── Events/            # Domain events (optional)
│   └── TaskCreatedEvent.cs
├── Exceptions/        # Domain-specific exceptions
│   └── TaskValidationException.cs
└── ValueObjects/      # Value objects (optional, DDD)
    └── EmailAddress.cs
```

#### Application Layer (`TaskAgent.Application`)

**Purpose**: Use cases, business workflows, and application logic. Defines interfaces for infrastructure.

```
TaskAgent.Application/
├── Constants/              # Application-level constants
│   └── ServiceConstants.cs
├── DTOs/                  # Data Transfer Objects
│   ├── Requests/
│   │   ├── ChatRequest.cs
│   │   └── CreateTaskRequest.cs
│   └── Responses/
│       ├── ChatResponse.cs
│       └── TaskResponse.cs
├── Functions/             # AI Agent function tools
│   └── TaskFunctions.cs
├── Interfaces/            # Abstractions for infrastructure
│   ├── ITaskRepository.cs
│   ├── ITaskAgentService.cs
│   └── IContentSafetyService.cs
├── Services/              # Application services (orchestration)
│   └── TaskAgentService.cs
├── Validators/            # Input validation (FluentValidation)
│   └── CreateTaskRequestValidator.cs
└── Mappings/             # Object mapping profiles (AutoMapper)
    └── TaskMappingProfile.cs
```

#### Infrastructure Layer (`TaskAgent.Infrastructure`)

**Purpose**: External concerns - database, external services, file system. Implements Application interfaces.

```
TaskAgent.Infrastructure/
├── Constants/                    # Infrastructure constants
│   ├── CacheKeys.cs
│   └── ConnectionStringKeys.cs
├── Data/                        # Database context and configurations
│   ├── TaskDbContext.cs
│   └── Configurations/
│       └── TaskItemConfiguration.cs
├── Migrations/                  # EF Core migrations
│   └── 20251012_InitialCreate.cs
├── Repositories/                # Repository implementations
│   └── TaskRepository.cs
├── Services/                    # External service implementations
│   ├── ContentSafetyService.cs
│   ├── EmailService.cs
│   └── AzureStorageService.cs
├── Models/                      # Infrastructure-specific models
│   ├── ContentSafetyConfig.cs
│   └── PromptShieldResponse.cs
└── DependencyInjection.cs      # Infrastructure service registration
```

#### WebApp/WebApi Layer (`TaskAgent.WebApp`)

**Purpose**: Entry point, API endpoints, middleware, UI (if applicable). Wires everything together.

```
TaskAgent.WebApp/
├── Constants/                   # Web-specific constants
│   └── ApiRoutes.cs
├── Controllers/                 # API Controllers
│   ├── ChatController.cs
│   ├── TaskController.cs
│   └── HomeController.cs
├── Middleware/                  # Custom middleware
│   └── ContentSafetyMiddleware.cs
├── Extensions/                  # Extension methods
│   ├── ConfigurationValidationExtensions.cs
│   └── ContentSafetyMiddlewareExtensions.cs
├── Models/                      # View models / API-specific models
│   └── ChatRequestDto.cs
├── Filters/                     # Action filters, exception filters
│   └── ValidationFilter.cs
├── Views/                       # Razor views (if MVC)
│   └── Home/
│       └── Index.cshtml
├── wwwroot/                     # Static files
│   ├── css/
│   ├── js/
│   └── lib/
├── Properties/
│   └── launchSettings.json
├── appsettings.json
├── appsettings.Development.json
├── DependencyInjection.cs      # WebApp service registration
└── Program.cs                   # Application entry point
```

### Folder Organization Best Practices

1. **Group by Feature (Alternative)**
   For larger applications, consider organizing by feature instead of by technical type:

   ```
   TaskAgent.Application/
   ├── Tasks/
   │   ├── Commands/
   │   │   └── CreateTask/
   │   │       ├── CreateTaskCommand.cs
   │   │       └── CreateTaskCommandHandler.cs
   │   ├── Queries/
   │   │   └── GetTaskById/
   │   │       ├── GetTaskByIdQuery.cs
   │   │       └── GetTaskByIdQueryHandler.cs
   │   └── DTOs/
   │       └── TaskDto.cs
   ```

2. **Naming Conventions**

   - Controllers: `{Entity}Controller.cs` (e.g., `TaskController.cs`)
   - Services: `{Entity}Service.cs` or `I{Entity}Service.cs` (interface)
   - Repositories: `{Entity}Repository.cs` or `I{Entity}Repository.cs`
   - DTOs: `{Entity}{Purpose}Dto.cs` (e.g., `TaskResponseDto.cs`)
   - Constants: Plural nouns or descriptive names (e.g., `ApiRoutes.cs`, `TaskConstants.cs`)

3. **File Naming**

   - Use PascalCase for all file names
   - Match the file name with the class name it contains
   - Example: `TaskItem.cs` contains `public class TaskItem`

4. **Avoid Generic Names**
   - ❌ `Helper.cs`, `Utility.cs`, `Manager.cs`
   - ✅ `DateTimeHelper.cs`, `StringUtility.cs`, `TaskManager.cs`

---

## Core Design Principles

### 1. DRY (Don't Repeat Yourself)

**Avoid duplicating code. Keep logic centralized to make your codebase easier to maintain.**

- Encapsulate repeated logic into reusable methods, classes, or services
- Use inheritance or composition to share common behavior
- Extract constants and configuration into centralized locations

**Example:**

```csharp
// ❌ BAD - Repeated validation logic
public class UserService
{
    public void CreateUser(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            throw new ArgumentException("Invalid email");
    }

    public void UpdateUser(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            throw new ArgumentException("Invalid email");
    }
}

// ✅ GOOD - Centralized validation
public class UserService
{
    private void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            throw new ArgumentException("Invalid email");
    }

    public void CreateUser(string email)
    {
        ValidateEmail(email);
        // Create logic
    }

    public void UpdateUser(string email)
    {
        ValidateEmail(email);
        // Update logic
    }
}
```

### 2. KISS (Keep It Simple, Stupid)

**Aim for simplicity in your solutions. Avoid overengineering or adding unnecessary layers.**

- Choose straightforward solutions over complex ones
- Avoid premature optimization
- Write code that is easy to understand and maintain
- Use clear naming and simple control flow

**Example:**

```csharp
// ❌ BAD - Over-engineered
public interface ITaskStatusStrategyFactory
{
    ITaskStatusStrategy CreateStrategy(TaskStatus status);
}

public interface ITaskStatusStrategy
{
    string GetDisplayText();
}

// ✅ GOOD - Simple and direct
public static class TaskStatusExtensions
{
    public static string GetDisplayText(this TaskStatus status)
    {
        return status switch
        {
            TaskStatus.Pending => "⏳ Pending",
            TaskStatus.InProgress => "🔄 In Progress",
            TaskStatus.Completed => "✅ Completed",
            _ => status.ToString()
        };
    }
}
```

### 3. YAGNI (You Aren't Gonna Need It)

**Only build what you need today. Don't waste time on hypothetical features that may never be used.**

- Implement features when they are actually required
- Avoid speculative generality
- Don't add hooks for future functionality that isn't planned
- Refactor when new requirements emerge

**Example:**

```csharp
// ❌ BAD - Building for unknown future needs
public class TaskRepository
{
    // We don't need sorting or filtering yet
    public Task<List<TaskItem>> GetAllAsync(
        string sortBy = null,
        string sortDirection = null,
        Dictionary<string, object> filters = null,
        int? skip = null,
        int? take = null)
    {
        // Complex implementation
    }
}

// ✅ GOOD - Build only what's needed now
public class TaskRepository
{
    public Task<List<TaskItem>> GetAllAsync()
    {
        return _context.Tasks.ToListAsync();
    }
}
```

### 4. LOD (Law of Demeter) - Principle of Least Knowledge

**Talk only to your immediate neighbors. Don't chain too many calls.**

- A method should only call methods of:
  - Itself
  - Its parameters
  - Objects it creates
  - Its direct dependencies

**Example:**

```csharp
// ❌ BAD - Violates Law of Demeter
public class TaskController
{
    public IActionResult GetTaskOwnerEmail(int taskId)
    {
        var task = _repository.GetTask(taskId);
        return Ok(task.Owner.Contact.Email); // Chain of calls
    }
}

// ✅ GOOD - Follows Law of Demeter
public class TaskController
{
    public IActionResult GetTaskOwnerEmail(int taskId)
    {
        var email = _repository.GetTaskOwnerEmail(taskId);
        return Ok(email);
    }
}

public class TaskRepository
{
    public string GetTaskOwnerEmail(int taskId)
    {
        // Encapsulates the navigation logic
        return _context.Tasks
            .Where(t => t.Id == taskId)
            .Select(t => t.Owner.Contact.Email)
            .FirstOrDefault();
    }
}
```

---

## SOLID Principles

### 5. SRP (Single Responsibility Principle)

**A class should have one responsibility only. Keep each component focused and cohesive.**

- Each class should have only one reason to change
- Separate concerns into different classes
- High cohesion within a class, low coupling between classes

**Example:**

```csharp
// ❌ BAD - Multiple responsibilities
public class TaskService
{
    public void CreateTask(TaskItem task)
    {
        // Validation
        if (string.IsNullOrEmpty(task.Title))
            throw new ArgumentException();

        // Save to database
        _context.Tasks.Add(task);
        _context.SaveChanges();

        // Send email notification
        var smtp = new SmtpClient();
        smtp.Send(new MailMessage("task@app.com", task.AssignedTo.Email));

        // Log activity
        File.AppendAllText("log.txt", $"Task created: {task.Id}");
    }
}

// ✅ GOOD - Separated responsibilities
public class TaskService
{
    private readonly ITaskRepository _repository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<TaskService> _logger;

    public async Task CreateTaskAsync(TaskItem task)
    {
        task.Validate(); // Validation in entity

        await _repository.AddAsync(task);
        await _notificationService.NotifyTaskCreatedAsync(task);
        _logger.LogInformation("Task created: {TaskId}", task.Id);
    }
}
```

### 6. OCP (Open/Closed Principle)

**Code should be open for extension but closed for modification.**

- Use abstraction to allow new behavior without changing existing code
- Prefer composition and polymorphism over conditional logic
- Use interfaces and abstract classes

**Example:**

```csharp
// ❌ BAD - Must modify class to add new export formats
public class TaskExporter
{
    public string Export(List<TaskItem> tasks, string format)
    {
        if (format == "json")
            return JsonConvert.SerializeObject(tasks);
        else if (format == "xml")
            return SerializeToXml(tasks);
        else if (format == "csv")
            return ConvertToCsv(tasks);
        // Must modify this method for each new format
    }
}

// ✅ GOOD - Open for extension, closed for modification
public interface ITaskExporter
{
    string Export(List<TaskItem> tasks);
}

public class JsonTaskExporter : ITaskExporter
{
    public string Export(List<TaskItem> tasks)
        => JsonConvert.SerializeObject(tasks);
}

public class XmlTaskExporter : ITaskExporter
{
    public string Export(List<TaskItem> tasks)
        => SerializeToXml(tasks);
}

public class TaskExportService
{
    private readonly Dictionary<string, ITaskExporter> _exporters;

    public TaskExportService(IEnumerable<ITaskExporter> exporters)
    {
        // Register exporters - new formats can be added without modification
    }
}
```

### 7. LSP (Liskov Substitution Principle)

**Subclasses must be usable in place of their parent classes without breaking functionality.**

- Derived classes must honor the contract of the base class
- Don't strengthen preconditions or weaken postconditions
- Maintain behavioral compatibility

**Example:**

```csharp
// ❌ BAD - Violates LSP
public class TaskItem
{
    public virtual void UpdateStatus(TaskStatus newStatus)
    {
        Status = newStatus;
    }
}

public class ReadOnlyTaskItem : TaskItem
{
    public override void UpdateStatus(TaskStatus newStatus)
    {
        throw new InvalidOperationException("Cannot modify read-only task");
        // Breaks substitutability - callers expect this to work
    }
}

// ✅ GOOD - Maintains substitutability
public abstract class TaskItem
{
    public TaskStatus Status { get; protected set; }

    public abstract bool CanUpdateStatus { get; }

    public void UpdateStatus(TaskStatus newStatus)
    {
        if (!CanUpdateStatus)
            throw new InvalidOperationException("Task status cannot be updated");

        Status = newStatus;
    }
}

public class EditableTaskItem : TaskItem
{
    public override bool CanUpdateStatus => true;
}

public class ReadOnlyTaskItem : TaskItem
{
    public override bool CanUpdateStatus => false;
}
```

### 8. ISP (Interface Segregation Principle)

**Design small, focused interfaces instead of large, general ones.**

- Clients should not depend on interfaces they don't use
- Split large interfaces into smaller, more specific ones
- Avoid "fat" interfaces

**Example:**

```csharp
// ❌ BAD - Fat interface
public interface ITaskRepository
{
    Task<TaskItem> GetByIdAsync(int id);
    Task<List<TaskItem>> GetAllAsync();
    Task AddAsync(TaskItem task);
    Task UpdateAsync(TaskItem task);
    Task DeleteAsync(int id);
    Task<List<TaskItem>> SearchAsync(string query);
    Task<byte[]> ExportToPdfAsync(int id);
    Task<TaskStatistics> GetStatisticsAsync();
    void BackupDatabase();
}

// ✅ GOOD - Segregated interfaces
public interface ITaskReader
{
    Task<TaskItem> GetByIdAsync(int id);
    Task<List<TaskItem>> GetAllAsync();
    Task<List<TaskItem>> SearchAsync(string query);
}

public interface ITaskWriter
{
    Task AddAsync(TaskItem task);
    Task UpdateAsync(TaskItem task);
    Task DeleteAsync(int id);
}

public interface ITaskExporter
{
    Task<byte[]> ExportToPdfAsync(int id);
}

public interface ITaskStatistics
{
    Task<TaskStatistics> GetStatisticsAsync();
}
```

### 9. DIP (Dependency Inversion Principle)

**High-level modules should not depend on low-level modules. Both should depend on abstractions.**

- Depend on interfaces or abstract classes, not concrete implementations
- Use dependency injection
- Invert control flow

**Example:**

```csharp
// ❌ BAD - High-level depends on low-level concrete class
public class TaskAgentService
{
    private readonly SqlServerTaskRepository _repository; // Concrete dependency
    private readonly SmtpEmailService _emailService; // Concrete dependency

    public TaskAgentService()
    {
        _repository = new SqlServerTaskRepository(); // Direct instantiation
        _emailService = new SmtpEmailService();
    }
}

// ✅ GOOD - Both depend on abstractions
public interface ITaskRepository
{
    Task<List<TaskItem>> GetAllAsync();
}

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}

public class TaskAgentService
{
    private readonly ITaskRepository _repository; // Abstraction
    private readonly IEmailService _emailService; // Abstraction

    public TaskAgentService(ITaskRepository repository, IEmailService emailService)
    {
        _repository = repository;
        _emailService = emailService;
    }
}

// Implementations
public class SqlServerTaskRepository : ITaskRepository { }
public class SmtpEmailService : IEmailService { }
```

---

## Clean Architecture Principles

### Layer Dependencies

**Dependencies should flow inward: UI → Application → Domain**

```
┌─────────────────────────────────────┐
│     Infrastructure & WebApp         │  ← External concerns
│  (DB, APIs, Controllers, Views)     │
├─────────────────────────────────────┤
│         Application Layer           │  ← Use cases, DTOs
│  (Services, Interfaces, Functions)  │
├─────────────────────────────────────┤
│          Domain Layer               │  ← Business logic
│    (Entities, Enums, Rules)         │  ← NO DEPENDENCIES
└─────────────────────────────────────┘
```

**Rules:**

1. **Domain Layer**: NO external dependencies, pure business logic
2. **Application Layer**: Defines interfaces (e.g., `ITaskRepository`)
3. **Infrastructure Layer**: Implements interfaces (e.g., `TaskRepository`)
4. **Web Layer**: Entry point, wires dependencies

**Example:**

```csharp
// ✅ Domain Layer (TaskAgent.Domain)
namespace TaskAgent.Domain.Entities
{
    public class TaskItem  // No dependencies on EF, JSON, etc.
    {
        public int Id { get; private set; }
        public string Title { get; private set; }

        private TaskItem() { } // For EF

        public static TaskItem Create(string title, string description)
        {
            // Business validation here
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title is required");

            return new TaskItem { Title = title };
        }
    }
}

// ✅ Application Layer (TaskAgent.Application)
namespace TaskAgent.Application.Interfaces
{
    public interface ITaskRepository  // Abstract contract
    {
        Task<TaskItem> GetByIdAsync(int id);
    }
}

// ✅ Infrastructure Layer (TaskAgent.Infrastructure)
namespace TaskAgent.Infrastructure.Repositories
{
    public class TaskRepository : ITaskRepository  // Concrete implementation
    {
        private readonly TaskDbContext _context;

        public async Task<TaskItem> GetByIdAsync(int id)
        {
            return await _context.Tasks.FindAsync(id);
        }
    }
}
```

### Domain-Driven Design (DDD) Patterns

#### Use Factory Methods for Entity Creation

```csharp
// ❌ BAD
public class TaskItem
{
    public string Title { get; set; }
}

var task = new TaskItem { Title = "Test" }; // No validation

// ✅ GOOD
public class TaskItem
{
    public string Title { get; private set; }

    private TaskItem() { } // EF requirement

    public static TaskItem Create(string title, string description, TaskPriority priority)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        if (title.Length > 200)
            throw new ArgumentException("Title cannot exceed 200 characters", nameof(title));

        return new TaskItem
        {
            Title = title,
            Description = description,
            Priority = priority,
            Status = TaskStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }
}

var task = TaskItem.Create("New Task", "Description", TaskPriority.High);
```

#### Encapsulate Business Rules in Entities

```csharp
// ✅ Business rules in the domain entity
public class TaskItem
{
    public void UpdateStatus(TaskStatus newStatus)
    {
        // Business rule: Cannot go from Completed to Pending
        if (Status == TaskStatus.Completed && newStatus == TaskStatus.Pending)
            throw new InvalidOperationException("Cannot reopen completed tasks");

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePriority(TaskPriority newPriority)
    {
        // Business rule: High priority tasks cannot be set to Completed status if overdue
        if (newPriority == TaskPriority.High && Status == TaskStatus.Completed && DueDate < DateTime.UtcNow)
            throw new InvalidOperationException("Cannot set overdue task to high priority");

        Priority = newPriority;
    }
}
```

---

## Clean Code Principles

### Naming Conventions

**Use meaningful, pronounceable, and searchable names:**

```csharp
// ❌ BAD
public class TskMgr
{
    public void PrcTsk(int id)
    {
        var t = GetT(id);
        var dt = DateTime.Now;
    }
}

// ✅ GOOD
public class TaskManager
{
    public void ProcessTask(int taskId)
    {
        var task = GetTask(taskId);
        var currentDateTime = DateTime.UtcNow;
    }
}
```

**Use PascalCase for classes, methods, properties:**

```csharp
public class TaskRepository
{
    public async Task<TaskItem> GetTaskByIdAsync(int id) { }
}
```

**Use camelCase for parameters and local variables:**

```csharp
public void CreateTask(string taskTitle, string description)
{
    var newTask = TaskItem.Create(taskTitle, description);
}
```

**Use UPPER_CASE for constants:**

```csharp
public static class TaskConstants
{
    public const int MAX_TITLE_LENGTH = 200;
    public const string DEFAULT_STATUS = "Pending";
}
```

### Functions Should Be Small and Do One Thing

```csharp
// ❌ BAD - Function does too much
public async Task<TaskItem> CreateAndNotifyTask(string title, string description, string userEmail)
{
    // Validate
    if (string.IsNullOrEmpty(title)) throw new ArgumentException();

    // Create
    var task = new TaskItem { Title = title, Description = description };
    await _context.Tasks.AddAsync(task);
    await _context.SaveChangesAsync();

    // Send email
    var smtp = new SmtpClient();
    await smtp.SendMailAsync(new MailMessage("system@app.com", userEmail, "Task Created", $"Task {title} created"));

    // Log
    _logger.LogInformation($"Task {task.Id} created");

    return task;
}

// ✅ GOOD - Separated concerns
public async Task<TaskItem> CreateTaskAsync(string title, string description)
{
    var task = TaskItem.Create(title, description);
    await _repository.AddAsync(task);
    return task;
}

public async Task NotifyTaskCreatedAsync(TaskItem task, string userEmail)
{
    await _notificationService.SendTaskCreatedEmailAsync(task, userEmail);
    _logger.LogInformation("Task {TaskId} created and notification sent", task.Id);
}
```

### Avoid Deep Nesting

```csharp
// ❌ BAD - Deep nesting
public async Task<TaskItem> GetTaskIfValid(int id, string userId)
{
    if (id > 0)
    {
        var task = await _repository.GetByIdAsync(id);
        if (task != null)
        {
            if (task.OwnerId == userId)
            {
                if (task.Status != TaskStatus.Deleted)
                {
                    return task;
                }
            }
        }
    }
    return null;
}

// ✅ GOOD - Early returns (Guard clauses)
public async Task<TaskItem> GetTaskIfValidAsync(int id, string userId)
{
    if (id <= 0)
        return null;

    var task = await _repository.GetByIdAsync(id);

    if (task == null)
        return null;

    if (task.OwnerId != userId)
        return null;

    if (task.Status == TaskStatus.Deleted)
        return null;

    return task;
}
```

### Use Descriptive Comments Only When Necessary

```csharp
// ❌ BAD - Obvious comments
public class TaskItem
{
    // This is the task ID
    public int Id { get; set; }

    // This method updates the status
    public void UpdateStatus(TaskStatus status)
    {
        // Set the status
        Status = status;
    }
}

// ✅ GOOD - Self-documenting code
public class TaskItem
{
    public int Id { get; private set; }

    /// <summary>
    /// Updates the task status with business rule validation.
    /// Cannot transition from Completed to Pending status.
    /// </summary>
    public void UpdateStatus(TaskStatus newStatus)
    {
        if (Status == TaskStatus.Completed && newStatus == TaskStatus.Pending)
            throw new InvalidOperationException("Cannot reopen completed tasks");

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### Error Handling

**Use exceptions for exceptional cases, not flow control:**

```csharp
// ❌ BAD - Exception for flow control
public TaskItem GetTask(int id)
{
    try
    {
        return _repository.GetById(id);
    }
    catch (NotFoundException)
    {
        return null; // Expected case, shouldn't use exception
    }
}

// ✅ GOOD - Exceptions for truly exceptional cases
public async Task<TaskItem> GetTaskAsync(int id)
{
    return await _repository.GetByIdAsync(id); // Returns null if not found
}

public async Task<TaskItem> GetRequiredTaskAsync(int id)
{
    var task = await _repository.GetByIdAsync(id);

    if (task == null)
        throw new TaskNotFoundException($"Task with ID {id} not found");

    return task;
}
```

---

## Additional Best Practices

### Immutability Where Possible

```csharp
// ✅ Use init or private set for properties
public class TaskItem
{
    public int Id { get; init; }
    public string Title { get; private set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

// ✅ Use records for DTOs
public record TaskResponseDto(int Id, string Title, string Status, DateTime CreatedAt);
```

### Async/Await Best Practices

```csharp
// ✅ Use async suffix for async methods
public async Task<TaskItem> GetTaskByIdAsync(int id)
{
    return await _repository.GetByIdAsync(id);
}

// ✅ Use ConfigureAwait(false) in library code
public async Task<TaskItem> GetTaskByIdAsync(int id)
{
    return await _repository.GetByIdAsync(id).ConfigureAwait(false);
}

// ✅ Don't use async void (except event handlers)
// ❌ BAD
public async void ProcessTask() { }

// ✅ GOOD
public async Task ProcessTaskAsync() { }
```

### Use Pattern Matching

```csharp
// ✅ Modern C# pattern matching
public string GetTaskStatusEmoji(TaskStatus status) => status switch
{
    TaskStatus.Pending => "⏳",
    TaskStatus.InProgress => "🔄",
    TaskStatus.Completed => "✅",
    _ => "❓"
};

public string GetPriorityEmoji(TaskPriority priority) => priority switch
{
    TaskPriority.Low => "🟢",
    TaskPriority.Medium => "🟡",
    TaskPriority.High => "🔴",
    _ => "⚪"
};
```

### Prefer Composition Over Inheritance

```csharp
// ❌ BAD - Deep inheritance hierarchies
public class Entity { }
public class BaseTask : Entity { }
public class WorkTask : BaseTask { }
public class UrgentWorkTask : WorkTask { }

// ✅ GOOD - Composition
public class TaskItem
{
    public TaskPriority Priority { get; private set; }
    public TaskStatus Status { get; private set; }
    public TaskMetadata Metadata { get; private set; }
}

public class TaskMetadata
{
    public string Category { get; set; }
    public List<string> Tags { get; set; }
}
```

### Repository Pattern Best Practices

```csharp
public interface ITaskRepository
{
    // ✅ Use async methods
    Task<TaskItem> GetByIdAsync(int id);
    Task<List<TaskItem>> GetAllAsync();

    // ✅ Use specific query methods instead of generic filters
    Task<List<TaskItem>> GetByStatusAsync(TaskStatus status);
    Task<List<TaskItem>> GetByPriorityAsync(TaskPriority priority);
    Task<List<TaskItem>> GetPendingTasksAsync();

    // ✅ Use explicit Add/Update/Delete
    Task AddAsync(TaskItem task);
    Task UpdateAsync(TaskItem task);
    Task DeleteAsync(int id);
}

public class TaskRepository : ITaskRepository
{
    private readonly TaskDbContext _context;

    // ✅ Use AsNoTracking for read-only queries
    public async Task<List<TaskItem>> GetAllAsync()
    {
        return await _context.Tasks
            .AsNoTracking()
            .ToListAsync();
    }

    // ✅ Use tracking for updates
    public async Task UpdateAsync(TaskItem task)
    {
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync();
    }
}
```

---

## Summary Checklist

When generating code, ensure:

- [ ] **Use Microsoft Learn MCP** - consult official docs for latest best practices
- [ ] **One class per file** (except related DTOs)
- [ ] **No nested classes** - each class in its own file
- [ ] **No magic numbers/strings** - use constants in appropriate layer
- [ ] **Proper folder structure** - follow Clean Architecture organization
- [ ] **No repeated code** (DRY) - extract common logic
- [ ] **Simple solutions** (KISS) - avoid overengineering
- [ ] **Only needed features** (YAGNI) - build for today
- [ ] **Minimal coupling** (LOD) - avoid long call chains
- [ ] **Single responsibility** (SRP) - one reason to change
- [ ] **Extensible design** (OCP) - open for extension
- [ ] **Proper substitution** (LSP) - subtypes are interchangeable
- [ ] **Focused interfaces** (ISP) - small, specific contracts
- [ ] **Depend on abstractions** (DIP) - use interfaces
- [ ] **Clean Architecture** - proper layer dependencies
- [ ] **Meaningful names** - clear and searchable
- [ ] **Small functions** - do one thing well
- [ ] **Guard clauses** - avoid deep nesting
- [ ] **Domain-driven** - business logic in entities
- [ ] **Async methods** - use async/await correctly
- [ ] **Immutability** - use private set or init
- [ ] **Error handling** - exceptions for exceptional cases
- [ ] **No .md files** - unless explicitly requested

---

_These guidelines ensure maintainable, testable, and scalable code following industry best practices and Microsoft recommendations._
