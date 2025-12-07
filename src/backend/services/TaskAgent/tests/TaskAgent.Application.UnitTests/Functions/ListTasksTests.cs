using Microsoft.Extensions.Logging.Abstractions;
using TaskAgent.Application.Functions;
using TaskAgent.Application.Interfaces;
using TaskAgent.Application.Telemetry;
using TaskAgent.Domain.Entities;
using TaskAgent.Domain.Enums;
using DomainTaskStatus = TaskAgent.Domain.Enums.TaskStatus;

namespace TaskAgent.Application.UnitTests.Functions;

/// <summary>
/// Unit tests for TaskFunctions.ListTasksAsync following AAA pattern
/// </summary>
public class ListTasksTests
{
    private readonly ITaskRepository _mockRepository;
    private readonly TaskFunctions _taskFunctions;

    public ListTasksTests()
    {
        _mockRepository = Substitute.For<ITaskRepository>();
        _taskFunctions = CreateTaskFunctions(_mockRepository);
    }

    #region Happy Path Tests

    [Fact]
    public async Task ListTasksAsync_WithNoFilters_ReturnsAllTasks()
    {
        // Arrange
        var tasks = new List<TaskItem>
        {
            TaskItem.Create("Task 1", "Description 1", TaskPriority.High),
            TaskItem.Create("Task 2", "Description 2", TaskPriority.Medium),
            TaskItem.Create("Task 3", "Description 3", TaskPriority.Low),
        };

        _mockRepository.SearchAsync(null, null).Returns(tasks);

        // Act
        string result = await _taskFunctions.ListTasksAsync();

        // Assert
        result.Should().Contain("Found 3 task(s)");
        result.Should().Contain("Task 1");
        result.Should().Contain("Task 2");
        result.Should().Contain("Task 3");
    }

    [Fact]
    public async Task ListTasksAsync_WithStatusFilter_ReturnsFilteredTasks()
    {
        // Arrange
        var task = TaskItem.Create("Pending Task", "Description", TaskPriority.Medium);
        var tasks = new List<TaskItem> { task };

        _mockRepository.SearchAsync(DomainTaskStatus.Pending, null).Returns(tasks);

        // Act
        string result = await _taskFunctions.ListTasksAsync(status: "Pending");

        // Assert
        result.Should().Contain("Found 1 task(s)");
        result.Should().Contain("Pending Task");
    }

    [Fact]
    public async Task ListTasksAsync_WithPriorityFilter_ReturnsFilteredTasks()
    {
        // Arrange
        var task = TaskItem.Create("High Priority Task", "Description", TaskPriority.High);
        var tasks = new List<TaskItem> { task };

        _mockRepository.SearchAsync(null, TaskPriority.High).Returns(tasks);

        // Act
        string result = await _taskFunctions.ListTasksAsync(priority: "High");

        // Assert
        result.Should().Contain("Found 1 task(s)");
        result.Should().Contain("High Priority Task");
    }

    [Fact]
    public async Task ListTasksAsync_WithBothFilters_ReturnsCombinedFilter()
    {
        // Arrange
        var task = TaskItem.Create("Filtered Task", "Description", TaskPriority.High);
        var tasks = new List<TaskItem> { task };

        _mockRepository.SearchAsync(DomainTaskStatus.Pending, TaskPriority.High).Returns(tasks);

        // Act
        string result = await _taskFunctions.ListTasksAsync(status: "Pending", priority: "High");

        // Assert
        result.Should().Contain("Found 1 task(s)");
        result.Should().Contain("Filtered Task");
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task ListTasksAsync_WithInvalidStatus_ReturnsErrorMessage()
    {
        // Arrange
        const string invalidStatus = "Unknown";

        // Act
        string result = await _taskFunctions.ListTasksAsync(status: invalidStatus);

        // Assert
        result.Should().Contain("Invalid status");
        result.Should().Contain(invalidStatus);
    }

    [Fact]
    public async Task ListTasksAsync_WithInvalidPriority_ReturnsErrorMessage()
    {
        // Arrange
        const string invalidPriority = "Critical";

        // Act
        string result = await _taskFunctions.ListTasksAsync(priority: invalidPriority);

        // Assert
        result.Should().Contain("Invalid priority");
        result.Should().Contain(invalidPriority);
    }

    #endregion

    #region Empty Results Tests

    [Fact]
    public async Task ListTasksAsync_WhenNoTasksExist_ReturnsNoTasksMessage()
    {
        // Arrange
        _mockRepository.SearchAsync(null, null).Returns(new List<TaskItem>());

        // Act
        string result = await _taskFunctions.ListTasksAsync();

        // Assert
        result.Should().Contain("No tasks found");
    }

    [Fact]
    public async Task ListTasksAsync_WhenNoTasksMatchFilter_ReturnsNoTasksMessage()
    {
        // Arrange
        _mockRepository.SearchAsync(DomainTaskStatus.Completed, null).Returns(new List<TaskItem>());

        // Act
        string result = await _taskFunctions.ListTasksAsync(status: "Completed");

        // Assert
        result.Should().Contain("No tasks found");
    }

    #endregion

    #region Emoji Display Tests

    [Fact]
    public async Task ListTasksAsync_DisplaysCorrectPriorityEmojis()
    {
        // Arrange
        var tasks = new List<TaskItem>
        {
            TaskItem.Create("High Priority", "Desc", TaskPriority.High),
            TaskItem.Create("Medium Priority", "Desc", TaskPriority.Medium),
            TaskItem.Create("Low Priority", "Desc", TaskPriority.Low),
        };

        _mockRepository.SearchAsync(null, null).Returns(tasks);

        // Act
        string result = await _taskFunctions.ListTasksAsync();

        // Assert
        result.Should().Contain("🔴"); // High
        result.Should().Contain("🟡"); // Medium
        result.Should().Contain("🟢"); // Low
    }

    [Fact]
    public async Task ListTasksAsync_DisplaysCorrectStatusEmojis()
    {
        // Arrange
        var task1 = TaskItem.Create("Pending", "Desc", TaskPriority.Medium);
        
        var task2 = TaskItem.Create("InProgress", "Desc", TaskPriority.Medium);
        task2.UpdateStatus(DomainTaskStatus.InProgress);
        
        var task3 = TaskItem.Create("Completed", "Desc", TaskPriority.Medium);
        task3.UpdateStatus(DomainTaskStatus.InProgress);
        task3.UpdateStatus(DomainTaskStatus.Completed);
        
        var tasks = new List<TaskItem> { task1, task2, task3 };

        _mockRepository.SearchAsync(null, null).Returns(tasks);

        // Act
        string result = await _taskFunctions.ListTasksAsync();

        // Assert
        result.Should().Contain("⏳"); // Pending
        result.Should().Contain("🔄"); // InProgress
        result.Should().Contain("✅"); // Completed
    }

    #endregion

    #region Helper Methods

    private static TaskFunctions CreateTaskFunctions(ITaskRepository mockRepository)
    {
        var services = new ServiceCollection();
        services.AddSingleton(mockRepository);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var metrics = new AgentMetrics();
        NullLogger<TaskFunctions> logger = NullLogger<TaskFunctions>.Instance;

        return new TaskFunctions(serviceProvider, metrics, logger);
    }

    #endregion
}
