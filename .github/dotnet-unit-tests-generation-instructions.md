# GitHub Copilot Custom Instructions - C#/.NET Unit Tests Generation

These instructions guide GitHub Copilot in generating clean, maintainable, and effective **unit tests** for C#/.NET projects following industry best practices, including the AAA pattern, proper naming conventions, and SOLID principles.

> **Note**: For integration tests (Testcontainers) and functional tests (WebApplicationFactory), see `dotnet-integration-functional-tests-generation-instructions.md`.

---

## Important Note

**DO NOT create `.md` documentation files with every prompt unless explicitly requested.**

## Using Microsoft Learn MCP

**Always use the Microsoft Learn MCP (Model Context Protocol) to consult up-to-date information** about:

- .NET unit testing best practices and latest features
- xUnit, NUnit, MSTest testing frameworks
- Mocking libraries (NSubstitute, Moq)
- Assertion libraries (FluentAssertions)

**When to use Microsoft Learn MCP:**

- Before implementing new test patterns
- When unsure about testing best practices
- To verify testing API signatures and methods
- To find official Microsoft test examples
- To ensure compliance with latest testing guidelines

**Available tools:**

- `microsoft_docs_search` - Search official Microsoft documentation
- `microsoft_code_sample_search` - Find official code examples
- `microsoft_docs_fetch` - Get complete documentation pages

---

## Testing Framework Stack

### Recommended Packages for Unit Tests

| Package | Version | Purpose |
|---------|---------|---------|
| **xUnit** | 2.9.x+ | Testing framework (Microsoft recommended) |
| **xunit.runner.visualstudio** | 3.x+ | Visual Studio/CLI test runner |
| **Microsoft.NET.Test.Sdk** | 17.x+ | Test SDK for .NET |
| **FluentAssertions** | 8.x+ | Readable assertion library |
| **NSubstitute** | 5.x+ | Mocking library |
| **Bogus** | 35.x+ | Fake data generator |
| **coverlet.collector** | 6.x+ | Code coverage |

### NuGet Package Configuration (Central Package Management)

```xml
<!-- Directory.Packages.props -->
<ItemGroup>
  <!-- Testing Framework -->
  <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
  <PackageVersion Include="xunit" Version="2.9.3" />
  <PackageVersion Include="xunit.runner.visualstudio" Version="3.1.4" />
  <PackageVersion Include="coverlet.collector" Version="6.0.4" />
  <!-- Testing Libraries -->
  <PackageVersion Include="FluentAssertions" Version="8.2.0" />
  <PackageVersion Include="NSubstitute" Version="5.3.0" />
  <PackageVersion Include="Bogus" Version="35.6.1" />
</ItemGroup>
```

---

## Type Inference Rules (`var` vs Explicit Types)

**The return type MUST be known from either the left side OR the right side of the assignment.**

### Use Explicit Type (Left Side)

When the right side does NOT clearly indicate the type:

```csharp
// ✅ GOOD - Type not obvious from right side
string result = await _taskFunctions.GetTaskSummaryAsync();
TaskItem? task = await _repository.GetByIdAsync(taskId);
int count = await _repository.CountAsync();
bool isValid = await _validator.ValidateAsync(input);

// Method return types are not obvious
IEnumerable<TaskItem> tasks = await _repository.SearchAsync(status, priority);
```

### Use `var` (Right Side)

When the right side clearly shows the type:

```csharp
// ✅ GOOD - Type obvious from right side (constructor)
var tasks = new List<TaskItem>();
var task = new TaskItem();
var mockRepository = Substitute.For<ITaskRepository>();
var stringBuilder = new StringBuilder();

// ✅ GOOD - Type obvious from right side (generic methods)
var tasks = new List<TaskItem>
{
    TaskItem.Create("Task 1", "Description", TaskPriority.High),
    TaskItem.Create("Task 2", "Description", TaskPriority.Low)
};

// ✅ GOOD - Type obvious from cast or as
var task = result as TaskItem;
var items = (List<TaskItem>)collection;
```

### Examples in Test Context

```csharp
[Fact]
public async Task CreateTaskAsync_WithValidInput_ReturnsSuccessMessage()
{
    // Arrange
    var mockRepository = Substitute.For<ITaskRepository>();           // ✅ var - new expression
    var taskFunctions = CreateTaskFunctions(mockRepository);          // ✅ var - factory method with clear name
    
    // Act
    string result = await taskFunctions.CreateTaskAsync(              // ✅ explicit - async method return
        "Test Task",
        "Test Description",
        "High"
    );
    
    // Assert
    result.Should().Contain("created");
}

[Fact]
public async Task GetAllAsync_ReturnsAllTasks()
{
    // Arrange
    var tasks = new List<TaskItem>                                    // ✅ var - new expression
    {
        TaskItem.Create("Task 1", "Desc 1", TaskPriority.High),
        TaskItem.Create("Task 2", "Desc 2", TaskPriority.Low)
    };
    
    _mockRepository.GetAllAsync().Returns(tasks);
    
    // Act
    IEnumerable<TaskItem> result = await _repository.GetAllAsync();   // ✅ explicit - interface return
    
    // Assert
    result.Should().HaveCount(2);
}
```

---

## AAA Pattern (Arrange-Act-Assert)

The **AAA pattern** is the fundamental structure for all unit tests.

### Pattern Structure

```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    // Set up all prerequisites and inputs
    
    // Act
    // Execute the method under test (single action)
    
    // Assert
    // Verify the expected outcome
}
```

### Guidelines

| Section | Purpose | Rules |
|---------|---------|-------|
| **Arrange** | Set up test prerequisites | Initialize objects, configure mocks, prepare test data |
| **Act** | Execute the method under test | **Single action only** - one method call |
| **Assert** | Verify expected outcomes | Use FluentAssertions for readable assertions |

### Example

```csharp
[Fact]
public async Task GetTaskDetailsAsync_WithValidId_ReturnsTaskDetails()
{
    // Arrange
    const int taskId = 1;
    var task = TaskItem.Create("Test Task", "Test Description", TaskPriority.High);
    _mockRepository.GetByIdAsync(taskId).Returns(task);

    // Act
    string result = await _taskFunctions.GetTaskDetailsAsync(taskId);

    // Assert
    result.Should().Contain("Task Details");
    result.Should().Contain("Test Task");
    result.Should().Contain("High");
}
```

### Anti-Patterns to Avoid

```csharp
// ❌ BAD - Multiple Acts
[Fact]
public void MultipleActs_Violation()
{
    // Arrange
    var calculator = new Calculator();
    
    // Act - WRONG: Multiple actions
    int result1 = calculator.Add(1, 2);
    int result2 = calculator.Add(3, 4);
    
    // Assert
    result1.Should().Be(3);
    result2.Should().Be(7);
}

// ✅ GOOD - Separate tests for each action
[Fact]
public void Add_WithOneAndTwo_ReturnsThree()
{
    // Arrange
    var calculator = new Calculator();
    
    // Act
    int result = calculator.Add(1, 2);
    
    // Assert
    result.Should().Be(3);
}

[Fact]
public void Add_WithThreeAndFour_ReturnsSeven()
{
    // Arrange
    var calculator = new Calculator();
    
    // Act
    int result = calculator.Add(3, 4);
    
    // Assert
    result.Should().Be(7);
}
```

---

## Test Naming Convention

### Standard Format

```
MethodName_Scenario_ExpectedBehavior
```

| Part | Description | Example |
|------|-------------|---------|
| **MethodName** | Name of the method being tested | `CreateTaskAsync` |
| **Scenario** | Condition or input being tested | `WithValidInput`, `WhenTaskNotFound` |
| **ExpectedBehavior** | Expected result or behavior | `ReturnsSuccessMessage`, `ThrowsArgumentException` |

### Examples

```csharp
// ✅ GOOD - Clear, descriptive names
[Fact]
public async Task CreateTaskAsync_WithValidInput_ReturnsSuccessMessage() { }

[Fact]
public async Task CreateTaskAsync_WithEmptyTitle_ReturnsValidationError() { }

[Fact]
public async Task GetTaskDetailsAsync_WhenTaskNotFound_ReturnsNotFoundMessage() { }

[Fact]
public async Task UpdateTaskAsync_FromCompletedToPending_ReturnsBusinessRuleError() { }

[Fact]
public void TaskItem_Create_WithNullTitle_ThrowsArgumentException() { }

[Fact]
public void TaskItem_UpdateStatus_ToCompleted_SetsStatusCorrectly() { }

// ❌ BAD - Vague or unclear names
[Fact]
public void Test1() { }

[Fact]
public void TestCreateTask() { }

[Fact]
public void CreateTask_Works() { }

[Fact]
public void ShouldWork() { }
```

### Alternative Naming Patterns

```csharp
// Pattern: Should_ExpectedBehavior_When_Scenario
[Fact]
public void Should_ReturnSuccessMessage_When_TaskCreatedWithValidInput() { }

// Pattern: Given_Precondition_When_Action_Then_ExpectedResult
[Fact]
public void Given_ValidTask_When_UpdateStatusCalled_Then_StatusIsUpdated() { }
```

---

## Test Organization

### Project Structure

```
tests/
├── TaskAgent.Domain.UnitTests/
│   ├── Entities/
│   │   └── TaskItemTests.cs
│   ├── Constants/
│   │   ├── TaskConstantsTests.cs
│   │   └── ValidationMessagesTests.cs
│   ├── GlobalUsings.cs
│   └── TaskAgent.Domain.UnitTests.csproj
│
├── TaskAgent.Application.UnitTests/
│   ├── Functions/
│   │   ├── CreateTaskTests.cs
│   │   ├── ListTasksTests.cs
│   │   ├── GetTaskDetailsTests.cs
│   │   ├── UpdateTaskTests.cs
│   │   ├── DeleteTaskTests.cs
│   │   └── GetTaskSummaryTests.cs
│   ├── Validators/
│   │   └── PaginationValidatorTests.cs
│   ├── GlobalUsings.cs
│   └── TaskAgent.Application.UnitTests.csproj
```

### Naming Convention for Test Projects

| Type | Suffix | Example |
|------|--------|---------|
| Unit Tests | `.UnitTests` | `TaskAgent.Domain.UnitTests` |

### Test Class Organization with Regions

```csharp
public class TaskItemTests
{
    #region Create Method Tests

    [Fact]
    public void Create_WithValidInput_ReturnsTaskItem() { }

    [Fact]
    public void Create_WithNullTitle_ThrowsArgumentException() { }

    #endregion

    #region UpdateStatus Method Tests

    [Fact]
    public void UpdateStatus_ToInProgress_SetsStatusCorrectly() { }

    [Fact]
    public void UpdateStatus_FromCompletedToPending_ThrowsInvalidOperationException() { }

    #endregion

    #region Helper Methods

    private static TaskItem CreateTestTask(
        string title = "Test Task",
        string description = "Test Description",
        TaskPriority priority = TaskPriority.Medium)
    {
        return TaskItem.Create(title, description, priority);
    }

    #endregion
}
```

---

## Global Usings

Create a `GlobalUsings.cs` file in each test project:

```csharp
// GlobalUsings.cs
global using FluentAssertions;
global using Microsoft.Extensions.DependencyInjection;
global using NSubstitute;
global using Xunit;
```

---

## Test Project Configuration

### Unit Test Project (.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <!-- Testing Framework -->
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="coverlet.collector" />
    <!-- Testing Libraries -->
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="NSubstitute" />
    <!-- Required for IServiceProvider -->
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\TaskAgent.Application\TaskAgent.Application.csproj" />
  </ItemGroup>

</Project>
```

---

## xUnit Attributes

### [Fact] - Simple Tests

Use for tests that don't require parameters:

```csharp
[Fact]
public void Create_WithValidInput_ReturnsTaskItem()
{
    // Arrange
    const string title = "Test Task";
    const string description = "Test Description";

    // Act
    TaskItem task = TaskItem.Create(title, description, TaskPriority.Medium);

    // Assert
    task.Should().NotBeNull();
    task.Title.Should().Be(title);
}
```

### [Theory] + [InlineData] - Parameterized Tests

Use for testing multiple scenarios with the same logic:

```csharp
[Theory]
[InlineData("Pending")]
[InlineData("InProgress")]
[InlineData("Completed")]
public async Task UpdateTaskAsync_WithValidStatus_Succeeds(string status)
{
    // Arrange
    const int taskId = 1;
    var task = TaskItem.Create("Test Task", "Description", TaskPriority.Medium);
    _mockRepository.GetByIdAsync(taskId).Returns(task);

    // Act
    string result = await _taskFunctions.UpdateTaskAsync(taskId, status: status);

    // Assert
    result.Should().Contain("updated");
}

[Theory]
[InlineData("", 0)]
[InlineData(",", 0)]
[InlineData("1,2,3", 6)]
public void Add_MultipleScenarios_ReturnsExpectedResult(string input, int expected)
{
    // Arrange
    var calculator = new StringCalculator();

    // Act
    int result = calculator.Add(input);

    // Assert
    result.Should().Be(expected);
}
```

### [Theory] + [MemberData] - Complex Test Data

Use for complex test data from a method or property:

```csharp
public class TaskItemTests
{
    public static IEnumerable<object[]> InvalidTitleTestData =>
        new List<object[]>
        {
            new object[] { null!, "Title cannot be null" },
            new object[] { "", "Title cannot be empty" },
            new object[] { "   ", "Title cannot be whitespace" },
            new object[] { new string('a', 201), "Title exceeds maximum length" }
        };

    [Theory]
    [MemberData(nameof(InvalidTitleTestData))]
    public void Create_WithInvalidTitle_ThrowsArgumentException(string title, string expectedMessage)
    {
        // Arrange & Act
        Action act = () => TaskItem.Create(title, "Description", TaskPriority.Medium);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"*{expectedMessage}*");
    }
}
```

### [Theory] + [ClassData] - External Test Data

Use for reusable test data across multiple test classes:

```csharp
// File: TestData/InvalidTaskTitleData.cs
public class InvalidTaskTitleData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { null!, typeof(ArgumentNullException) };
        yield return new object[] { "", typeof(ArgumentException) };
        yield return new object[] { "   ", typeof(ArgumentException) };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

// File: TaskItemTests.cs
[Theory]
[ClassData(typeof(InvalidTaskTitleData))]
public void Create_WithInvalidTitle_ThrowsExpectedException(string title, Type exceptionType)
{
    // Arrange & Act
    Action act = () => TaskItem.Create(title, "Description", TaskPriority.Medium);

    // Assert
    act.Should().Throw<Exception>().Which.Should().BeOfType(exceptionType);
}
```

---

## FluentAssertions Patterns

### Basic Assertions

```csharp
// Strings
result.Should().NotBeNullOrEmpty();
result.Should().Be("expected value");
result.Should().Contain("substring");
result.Should().StartWith("prefix");
result.Should().EndWith("suffix");
result.Should().MatchRegex(@"\d{4}-\d{2}-\d{2}");

// Numbers
count.Should().Be(5);
count.Should().BeGreaterThan(0);
count.Should().BeInRange(1, 10);
count.Should().BePositive();

// Booleans
isValid.Should().BeTrue();
isCompleted.Should().BeFalse();

// Nulls
task.Should().NotBeNull();
task.Should().BeNull();
```

### Collection Assertions

```csharp
// Count
tasks.Should().HaveCount(3);
tasks.Should().BeEmpty();
tasks.Should().NotBeEmpty();
tasks.Should().HaveCountGreaterThan(0);
tasks.Should().ContainSingle();

// Content
tasks.Should().Contain(expectedTask);
tasks.Should().NotContain(unexpectedTask);
tasks.Should().AllSatisfy(t => t.Status.Should().Be(TaskStatus.Pending));
tasks.Should().OnlyContain(t => t.Priority == TaskPriority.High);

// Order
tasks.Should().BeInAscendingOrder(t => t.CreatedAt);
tasks.Should().BeInDescendingOrder(t => t.Priority);
```

### Object Assertions

```csharp
// Type
result.Should().BeOfType<TaskItem>();
result.Should().BeAssignableTo<IEntity>();

// Equivalence (deep comparison)
actualTask.Should().BeEquivalentTo(expectedTask);
actualTask.Should().BeEquivalentTo(expectedTask, options => 
    options.Excluding(t => t.Id)
           .Excluding(t => t.CreatedAt));
```

### Exception Assertions

```csharp
// Synchronous
Action act = () => TaskItem.Create(null!, "Description", TaskPriority.High);

act.Should().Throw<ArgumentException>()
    .WithMessage("*Title*required*");

act.Should().Throw<ArgumentException>()
    .Where(ex => ex.ParamName == "title");

// Asynchronous
Func<Task> act = async () => await _service.CreateAsync(invalidInput);

await act.Should().ThrowAsync<ValidationException>()
    .WithMessage("*validation failed*");

// Should NOT throw
Action act = () => TaskItem.Create("Valid Title", "Description", TaskPriority.High);
act.Should().NotThrow();

Func<Task> asyncAct = async () => await _service.GetByIdAsync(1);
await asyncAct.Should().NotThrowAsync();
```

### DateTime Assertions

```csharp
task.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
task.CreatedAt.Should().BeBefore(DateTime.UtcNow);
task.UpdatedAt.Should().BeAfter(task.CreatedAt);
```

---

## NSubstitute Mocking Patterns

### Basic Mocking

```csharp
// Create mock
var mockRepository = Substitute.For<ITaskRepository>();

// Setup return value
mockRepository.GetByIdAsync(1).Returns(expectedTask);
mockRepository.GetByIdAsync(Arg.Any<int>()).Returns(expectedTask);

// Setup async return
mockRepository.GetAllAsync().Returns(Task.FromResult<IEnumerable<TaskItem>>(taskList));

// Shorthand for async
mockRepository.GetAllAsync().Returns(taskList);
```

### Argument Matchers

```csharp
// Any value
mockRepository.GetByIdAsync(Arg.Any<int>()).Returns(task);

// Specific condition
mockRepository.SearchAsync(
    Arg.Is<TaskStatus?>(s => s == TaskStatus.Pending),
    Arg.Any<TaskPriority?>()
).Returns(filteredTasks);

// Capture argument
int capturedId = 0;
mockRepository.GetByIdAsync(Arg.Do<int>(x => capturedId = x)).Returns(task);
```

### Verifying Calls

```csharp
// Verify method was called
await mockRepository.Received(1).GetByIdAsync(1);
await mockRepository.Received().SaveChangesAsync();

// Verify method was NOT called
await mockRepository.DidNotReceive().DeleteAsync(Arg.Any<int>());

// Verify call order
Received.InOrder(() =>
{
    mockRepository.GetByIdAsync(taskId);
    mockRepository.UpdateAsync(Arg.Any<TaskItem>());
    mockRepository.SaveChangesAsync();
});
```

### Throwing Exceptions

```csharp
mockRepository.GetByIdAsync(999).Returns<TaskItem?>(x => 
    throw new NotFoundException("Task not found"));

// Or simpler
mockRepository.GetByIdAsync(999).Throws(new NotFoundException("Task not found"));
```

### Returning Different Values

```csharp
// Return different values on subsequent calls
mockRepository.GetByIdAsync(1)
    .Returns(task1, task2, task3);

// Return based on argument
mockRepository.GetByIdAsync(Arg.Any<int>())
    .Returns(callInfo => 
    {
        int id = callInfo.Arg<int>();
        return tasks.FirstOrDefault(t => t.Id == id);
    });
```

---

## Test Helper Methods

### Factory Methods for Test Objects

```csharp
public class TaskFunctionsTests
{
    private readonly ITaskRepository _mockRepository;
    private readonly TaskFunctions _taskFunctions;

    public TaskFunctionsTests()
    {
        _mockRepository = Substitute.For<ITaskRepository>();
        _taskFunctions = CreateTaskFunctions(_mockRepository);
    }

    #region Helper Methods

    private static TaskFunctions CreateTaskFunctions(ITaskRepository mockRepository)
    {
        var services = new ServiceCollection();
        services.AddSingleton(mockRepository);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var metrics = new AgentMetrics();
        var logger = NullLogger<TaskFunctions>.Instance;

        return new TaskFunctions(serviceProvider, metrics, logger);
    }

    private static TaskItem CreateTestTask(
        string title = "Test Task",
        string description = "Test Description",
        TaskPriority priority = TaskPriority.Medium)
    {
        return TaskItem.Create(title, description, priority);
    }

    private static List<TaskItem> CreateTestTasks(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => TaskItem.Create($"Task {i}", $"Description {i}", TaskPriority.Medium))
            .ToList();
    }

    #endregion
}
```

### Bogus Fake Data Generator

```csharp
public static class TaskItemFaker
{
    private static readonly Faker<TaskItem> _faker = new Faker<TaskItem>()
        .CustomInstantiator(f => TaskItem.Create(
            f.Lorem.Sentence(3),
            f.Lorem.Paragraph(),
            f.PickRandom<TaskPriority>()
        ));

    public static TaskItem Generate() => _faker.Generate();

    public static List<TaskItem> Generate(int count) => _faker.Generate(count);
}

// Usage in tests
[Fact]
public async Task ListTasksAsync_WithMultipleTasks_ReturnsAllTasks()
{
    // Arrange
    List<TaskItem> tasks = TaskItemFaker.Generate(10);
    _mockRepository.GetAllAsync().Returns(tasks);

    // Act
    string result = await _taskFunctions.ListTasksAsync();

    // Assert
    result.Should().Contain("10 task(s)");
}
```

---

## What to Unit Test

- ✅ Domain entities and value objects
- ✅ Application services and use cases
- ✅ Business logic and rules
- ✅ Validators
- ✅ Mappers and converters
- ✅ Extension methods
- ✅ Helper/utility classes

### Unit Tests Characteristics

| Aspect | Unit Tests |
|--------|------------|
| **Dependencies** | Mocked |
| **Speed** | Fast (ms) |
| **Isolation** | Complete |
| **Database** | No |
| **External APIs** | No |
| **Project Suffix** | `.UnitTests` |

---

## Common Anti-Patterns to Avoid

### ❌ Testing Implementation Details

```csharp
// ❌ BAD - Testing private methods
[Fact]
public void ValidateTitle_WithValidTitle_ReturnsTrue()
{
    // Don't test private methods directly
}

// ✅ GOOD - Test through public API
[Fact]
public void Create_WithValidTitle_ReturnsTaskItem()
{
    TaskItem task = TaskItem.Create("Valid Title", "Description", TaskPriority.High);
    task.Should().NotBeNull();
}
```

### ❌ Multiple Assertions Without Clear Purpose

```csharp
// ❌ BAD - Too many unrelated assertions
[Fact]
public void Create_DoesEverything()
{
    TaskItem task = TaskItem.Create("Title", "Description", TaskPriority.High);
    
    task.Should().NotBeNull();
    task.Title.Should().Be("Title");
    task.Description.Should().Be("Description");
    task.Priority.Should().Be(TaskPriority.High);
    task.Status.Should().Be(TaskStatus.Pending);
    task.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    task.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    task.Id.Should().Be(0);
}

// ✅ GOOD - Focused tests
[Fact]
public void Create_WithValidInput_SetsTitle()
{
    TaskItem task = TaskItem.Create("My Title", "Description", TaskPriority.High);
    task.Title.Should().Be("My Title");
}

[Fact]
public void Create_WithValidInput_SetsInitialStatusToPending()
{
    TaskItem task = TaskItem.Create("Title", "Description", TaskPriority.High);
    task.Status.Should().Be(TaskStatus.Pending);
}
```

### ❌ Logic in Tests

```csharp
// ❌ BAD - Logic in test
[Fact]
public void ProcessTasks_WithMultipleTasks()
{
    var tasks = new List<TaskItem>();
    for (int i = 0; i < 5; i++)
    {
        if (i % 2 == 0)
            tasks.Add(TaskItem.Create($"Even Task {i}", "Desc", TaskPriority.High));
        else
            tasks.Add(TaskItem.Create($"Odd Task {i}", "Desc", TaskPriority.Low));
    }
    
    // Test logic...
}

// ✅ GOOD - Use [Theory] for multiple scenarios
[Theory]
[InlineData(TaskPriority.High)]
[InlineData(TaskPriority.Medium)]
[InlineData(TaskPriority.Low)]
public void Create_WithDifferentPriorities_SetsPriorityCorrectly(TaskPriority priority)
{
    TaskItem task = TaskItem.Create("Title", "Description", priority);
    task.Priority.Should().Be(priority);
}
```

### ❌ Shared Mutable State

```csharp
// ❌ BAD - Shared state between tests
public class TaskServiceTests
{
    private static TaskItem _sharedTask = TaskItem.Create("Shared", "Desc", TaskPriority.High);

    [Fact]
    public void Test1()
    {
        _sharedTask.UpdateStatus(TaskStatus.InProgress);
        // This affects Test2!
    }

    [Fact]
    public void Test2()
    {
        // _sharedTask might be in unexpected state
    }
}

// ✅ GOOD - Fresh state for each test
public class TaskServiceTests
{
    [Fact]
    public void Test1()
    {
        var task = TaskItem.Create("Task 1", "Desc", TaskPriority.High);
        task.UpdateStatus(TaskStatus.InProgress);
        // Isolated from other tests
    }

    [Fact]
    public void Test2()
    {
        var task = TaskItem.Create("Task 2", "Desc", TaskPriority.High);
        // Fresh task, no pollution from Test1
    }
}
```

---

## Summary Checklist

When generating unit tests, ensure:

- [ ] **AAA Pattern** - Clear Arrange, Act, Assert sections
- [ ] **Single Act** - One action per test
- [ ] **Descriptive Names** - `MethodName_Scenario_ExpectedBehavior`
- [ ] **Type Inference** - Explicit types when return type not obvious, `var` when obvious
- [ ] **No Magic Strings** - Use constants for test data
- [ ] **No Test Logic** - Avoid if/for/while in tests
- [ ] **Focused Tests** - Test one thing per test
- [ ] **FluentAssertions** - Readable assertion syntax
- [ ] **NSubstitute** - Proper mocking patterns
- [ ] **Helper Methods** - Reusable test utilities
- [ ] **Regions** - Organize tests by method/feature
- [ ] **Isolation** - No shared mutable state
- [ ] **Fast** - Unit tests should run in milliseconds
- [ ] **Deterministic** - Same result every time

---

_These guidelines ensure maintainable, readable, and effective unit tests following Microsoft and industry best practices._
