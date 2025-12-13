using Microsoft.Extensions.Logging.Abstractions;
using TaskAgent.Application.Constants;
using TaskAgent.Application.Functions;
using TaskAgent.Application.Interfaces;
using TaskAgent.Application.Telemetry;
using TaskAgent.Domain.Entities;
using TaskAgent.Domain.Enums;
using DomainTaskStatus = TaskAgent.Domain.Enums.TaskStatus;

namespace TaskAgent.Application.UnitTests.Functions;

/// <summary>
/// Unit tests for TaskFunctions.GetTaskSummaryAsync following AAA pattern
/// </summary>
public class GetTaskSummaryTests
{
    private const string TestUserId = "test-user-id-12345";
    private readonly ITaskRepository _mockRepository;
    private readonly IUserContext _mockUserContext;
    private readonly TaskFunctions _taskFunctions;

    public GetTaskSummaryTests()
    {
        _mockRepository = Substitute.For<ITaskRepository>();
        _mockUserContext = Substitute.For<IUserContext>();
        _mockUserContext.UserId.Returns(TestUserId);
        _mockUserContext.IsAuthenticated.Returns(true);
        _taskFunctions = CreateTaskFunctions(_mockRepository, _mockUserContext);
    }

    #region Happy Path Tests

    [Fact]
    public async Task GetTaskSummaryAsync_WithTasks_ReturnsSummary()
    {
        // Arrange
        var tasks = new List<TaskItem>
        {
            TaskItem.Create("Task 1", "Desc 1", TaskPriority.High, TestUserId),
            TaskItem.Create("Task 2", "Desc 2", TaskPriority.Medium, TestUserId),
            TaskItem.Create("Task 3", "Desc 3", TaskPriority.Low, TestUserId)
        };
        
        _mockRepository.GetAllByUserAsync(TestUserId).Returns(tasks);

        // Act
        string result = await _taskFunctions.GetTaskSummaryAsync();

        // Assert
        result.Should().Contain("Task Summary");
        result.Should().Contain("Total Tasks: 3");
    }

    [Fact]
    public async Task GetTaskSummaryAsync_ReturnsStatusBreakdown()
    {
        // Arrange
        var pendingTask = TaskItem.Create("Pending", "Desc", TaskPriority.Low, TestUserId);
        
        var inProgressTask = TaskItem.Create("InProgress", "Desc", TaskPriority.Medium, TestUserId);
        inProgressTask.UpdateStatus(DomainTaskStatus.InProgress);
        
        var completedTask = TaskItem.Create("Completed", "Desc", TaskPriority.High, TestUserId);
        completedTask.UpdateStatus(DomainTaskStatus.Completed);

        var tasks = new List<TaskItem> { pendingTask, inProgressTask, completedTask };
        _mockRepository.GetAllByUserAsync(TestUserId).Returns(tasks);

        // Act
        string result = await _taskFunctions.GetTaskSummaryAsync();

        // Assert
        result.Should().Contain("Pending: 1");
        result.Should().Contain("In Progress: 1");
        result.Should().Contain("Completed: 1");
    }

    [Fact]
    public async Task GetTaskSummaryAsync_ReturnsHighPriorityCount()
    {
        // Arrange
        var highPriorityTask1 = TaskItem.Create("High 1", "Desc", TaskPriority.High, TestUserId);
        var highPriorityTask2 = TaskItem.Create("High 2", "Desc", TaskPriority.High, TestUserId);
        var mediumPriorityTask = TaskItem.Create("Medium", "Desc", TaskPriority.Medium, TestUserId);

        var tasks = new List<TaskItem> { highPriorityTask1, highPriorityTask2, mediumPriorityTask };
        _mockRepository.GetAllByUserAsync(TestUserId).Returns(tasks);

        // Act
        string result = await _taskFunctions.GetTaskSummaryAsync();

        // Assert
        result.Should().Contain("High Priority (not completed): 2");
    }

    [Fact]
    public async Task GetTaskSummaryAsync_ExcludesCompletedFromHighPriorityCount()
    {
        // Arrange
        var highPriorityPending = TaskItem.Create("High Pending", "Desc", TaskPriority.High, TestUserId);
        var highPriorityCompleted = TaskItem.Create("High Completed", "Desc", TaskPriority.High, TestUserId);
        highPriorityCompleted.UpdateStatus(DomainTaskStatus.Completed);

        var tasks = new List<TaskItem> { highPriorityPending, highPriorityCompleted };
        _mockRepository.GetAllByUserAsync(TestUserId).Returns(tasks);

        // Act
        string result = await _taskFunctions.GetTaskSummaryAsync();

        // Assert
        result.Should().Contain("High Priority (not completed): 1");
    }

    [Fact]
    public async Task GetTaskSummaryAsync_CalculatesCompletionRate()
    {
        // Arrange
        var pendingTask = TaskItem.Create("Pending", "Desc", TaskPriority.Low, TestUserId);
        var completedTask = TaskItem.Create("Completed", "Desc", TaskPriority.Medium, TestUserId);
        completedTask.UpdateStatus(DomainTaskStatus.Completed);

        var tasks = new List<TaskItem> { pendingTask, completedTask };
        _mockRepository.GetAllByUserAsync(TestUserId).Returns(tasks);

        // Act
        string result = await _taskFunctions.GetTaskSummaryAsync();

        // Assert
        result.Should().Contain("Completion Rate: 50%");
    }

    [Fact]
    public async Task GetTaskSummaryAsync_With100PercentCompletion_ShowsCorrectRate()
    {
        // Arrange
        var task1 = TaskItem.Create("Task 1", "Desc", TaskPriority.Low, TestUserId);
        task1.UpdateStatus(DomainTaskStatus.Completed);
        
        var task2 = TaskItem.Create("Task 2", "Desc", TaskPriority.Medium, TestUserId);
        task2.UpdateStatus(DomainTaskStatus.Completed);

        var tasks = new List<TaskItem> { task1, task2 };
        _mockRepository.GetAllByUserAsync(TestUserId).Returns(tasks);

        // Act
        string result = await _taskFunctions.GetTaskSummaryAsync();

        // Assert
        result.Should().Contain("Completion Rate: 100%");
    }

    [Fact]
    public async Task GetTaskSummaryAsync_With0PercentCompletion_ShowsCorrectRate()
    {
        // Arrange
        var task1 = TaskItem.Create("Task 1", "Desc", TaskPriority.Low, TestUserId);
        var task2 = TaskItem.Create("Task 2", "Desc", TaskPriority.Medium, TestUserId);

        var tasks = new List<TaskItem> { task1, task2 };
        _mockRepository.GetAllByUserAsync(TestUserId).Returns(tasks);

        // Act
        string result = await _taskFunctions.GetTaskSummaryAsync();

        // Assert
        result.Should().Contain("Completion Rate: 0%");
    }

    #endregion

    #region Empty State Tests

    [Fact]
    public async Task GetTaskSummaryAsync_WithNoTasks_ReturnsNoTasksMessage()
    {
        // Arrange
        _mockRepository.GetAllByUserAsync(TestUserId).Returns(new List<TaskItem>());

        // Act
        string result = await _taskFunctions.GetTaskSummaryAsync();

        // Assert
        result.Should().Contain(ErrorMessages.NO_TASKS_IN_SYSTEM);
    }

    [Fact]
    public async Task GetTaskSummaryAsync_WithEmptyList_DoesNotThrowException()
    {
        // Arrange
        _mockRepository.GetAllByUserAsync(TestUserId).Returns(new List<TaskItem>());

        // Act
        Func<Task<string>> act = async () => await _taskFunctions.GetTaskSummaryAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Format Tests

    [Fact]
    public async Task GetTaskSummaryAsync_ContainsExpectedEmojis()
    {
        // Arrange
        var tasks = new List<TaskItem>
        {
            TaskItem.Create("Task 1", "Desc", TaskPriority.High, TestUserId)
        };
        _mockRepository.GetAllByUserAsync(TestUserId).Returns(tasks);

        // Act
        string result = await _taskFunctions.GetTaskSummaryAsync();

        // Assert
        result.Should().Contain("📊"); // Summary emoji
        result.Should().Contain("⏳"); // Pending emoji
        result.Should().Contain("🔄"); // In Progress emoji
        result.Should().Contain("✅"); // Completed emoji
        result.Should().Contain("🔴"); // High Priority emoji
    }

    [Fact]
    public async Task GetTaskSummaryAsync_ContainsAllSections()
    {
        // Arrange
        var tasks = new List<TaskItem>
        {
            TaskItem.Create("Task 1", "Desc", TaskPriority.High, TestUserId)
        };
        _mockRepository.GetAllByUserAsync(TestUserId).Returns(tasks);

        // Act
        string result = await _taskFunctions.GetTaskSummaryAsync();

        // Assert
        result.Should().Contain("Total Tasks:");
        result.Should().Contain("By Status:");
        result.Should().Contain("Pending:");
        result.Should().Contain("In Progress:");
        result.Should().Contain("Completed:");
        result.Should().Contain("High Priority");
        result.Should().Contain("Completion Rate:");
    }

    #endregion

    #region Repository Interaction Tests

    [Fact]
    public async Task GetTaskSummaryAsync_CallsRepositoryGetAllByUserAsync()
    {
        // Arrange
        _mockRepository.GetAllByUserAsync(TestUserId).Returns(new List<TaskItem>());

        // Act
        await _taskFunctions.GetTaskSummaryAsync();

        // Assert
        await _mockRepository.Received(1).GetAllByUserAsync(TestUserId);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetTaskSummaryAsync_WithSingleTask_ShowsCorrectSummary()
    {
        // Arrange
        var tasks = new List<TaskItem>
        {
            TaskItem.Create("Single Task", "Desc", TaskPriority.High, TestUserId)
        };
        _mockRepository.GetAllByUserAsync(TestUserId).Returns(tasks);

        // Act
        string result = await _taskFunctions.GetTaskSummaryAsync();

        // Assert
        result.Should().Contain("Total Tasks: 1");
        result.Should().Contain("Pending: 1");
        result.Should().Contain("In Progress: 0");
        result.Should().Contain("Completed: 0");
        result.Should().Contain("High Priority (not completed): 1");
    }

    [Fact]
    public async Task GetTaskSummaryAsync_WithAllStatusTypes_CountsCorrectly()
    {
        // Arrange
        var pending1 = TaskItem.Create("Pending 1", "Desc", TaskPriority.Low, TestUserId);
        var pending2 = TaskItem.Create("Pending 2", "Desc", TaskPriority.Low, TestUserId);
        
        var inProgress1 = TaskItem.Create("InProgress 1", "Desc", TaskPriority.Medium, TestUserId);
        inProgress1.UpdateStatus(DomainTaskStatus.InProgress);
        var inProgress2 = TaskItem.Create("InProgress 2", "Desc", TaskPriority.Medium, TestUserId);
        inProgress2.UpdateStatus(DomainTaskStatus.InProgress);
        var inProgress3 = TaskItem.Create("InProgress 3", "Desc", TaskPriority.High, TestUserId);
        inProgress3.UpdateStatus(DomainTaskStatus.InProgress);
        
        var completed1 = TaskItem.Create("Completed 1", "Desc", TaskPriority.High, TestUserId);
        completed1.UpdateStatus(DomainTaskStatus.Completed);

        var tasks = new List<TaskItem> { pending1, pending2, inProgress1, inProgress2, inProgress3, completed1 };
        _mockRepository.GetAllByUserAsync(TestUserId).Returns(tasks);

        // Act
        string result = await _taskFunctions.GetTaskSummaryAsync();

        // Assert
        result.Should().Contain("Total Tasks: 6");
        result.Should().Contain("Pending: 2");
        result.Should().Contain("In Progress: 3");
        result.Should().Contain("Completed: 1");
    }

    #endregion

    #region Helper Methods

    private static TaskFunctions CreateTaskFunctions(ITaskRepository mockRepository, IUserContext mockUserContext)
    {
        var services = new ServiceCollection();
        services.AddSingleton(mockRepository);
        services.AddSingleton(mockUserContext);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var metrics = new AgentMetrics();
        NullLogger<TaskFunctions> logger = NullLogger<TaskFunctions>.Instance;

        return new TaskFunctions(serviceProvider, metrics, logger);
    }

    #endregion
}
