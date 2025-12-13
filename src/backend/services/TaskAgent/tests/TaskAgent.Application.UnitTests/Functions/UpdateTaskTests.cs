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
/// Unit tests for TaskFunctions.UpdateTaskAsync following AAA pattern
/// </summary>
public class UpdateTaskTests
{
    private const string TestUserId = "test-user-id-12345";
    private readonly ITaskRepository _mockRepository;
    private readonly IUserContext _mockUserContext;
    private readonly TaskFunctions _taskFunctions;

    public UpdateTaskTests()
    {
        _mockRepository = Substitute.For<ITaskRepository>();
        _mockUserContext = Substitute.For<IUserContext>();
        _mockUserContext.UserId.Returns(TestUserId);
        _mockUserContext.IsAuthenticated.Returns(true);
        _taskFunctions = CreateTaskFunctions(_mockRepository, _mockUserContext);
    }

    #region Happy Path Tests

    [Fact]
    public async Task UpdateTaskAsync_WithValidStatus_UpdatesTaskStatus()
    {
        // Arrange
        const int taskId = 1;
        var task = TaskItem.Create("Test Task", "Description", TaskPriority.Medium, TestUserId);
        
        _mockRepository.GetByIdAsync(taskId, TestUserId).Returns(task);

        // Act
        string result = await _taskFunctions.UpdateTaskAsync(taskId, status: "InProgress");

        // Assert
        result.Should().Contain("updated");
        result.Should().Contain("status");
        result.Should().Contain("InProgress");
        await _mockRepository.Received(1).UpdateAsync(task);
        await _mockRepository.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task UpdateTaskAsync_WithValidPriority_UpdatesTaskPriority()
    {
        // Arrange
        const int taskId = 1;
        var task = TaskItem.Create("Test Task", "Description", TaskPriority.Low, TestUserId);
        
        _mockRepository.GetByIdAsync(taskId, TestUserId).Returns(task);

        // Act
        string result = await _taskFunctions.UpdateTaskAsync(taskId, priority: "High");

        // Assert
        result.Should().Contain("updated");
        result.Should().Contain("priority");
        result.Should().Contain("High");
        await _mockRepository.Received(1).UpdateAsync(task);
        await _mockRepository.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task UpdateTaskAsync_WithBothStatusAndPriority_UpdatesBothFields()
    {
        // Arrange
        const int taskId = 1;
        var task = TaskItem.Create("Test Task", "Description", TaskPriority.Low, TestUserId);
        
        _mockRepository.GetByIdAsync(taskId, TestUserId).Returns(task);

        // Act
        string result = await _taskFunctions.UpdateTaskAsync(taskId, status: "InProgress", priority: "High");

        // Assert
        result.Should().Contain("updated");
        result.Should().Contain("status");
        result.Should().Contain("priority");
        await _mockRepository.Received(1).UpdateAsync(task);
        await _mockRepository.Received(1).SaveChangesAsync();
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("InProgress")]
    [InlineData("Completed")]
    public async Task UpdateTaskAsync_WithAllValidStatuses_Succeeds(string status)
    {
        // Arrange
        const int taskId = 1;
        var task = TaskItem.Create("Test Task", "Description", TaskPriority.Medium, TestUserId);
        
        _mockRepository.GetByIdAsync(taskId, TestUserId).Returns(task);

        // Act
        string result = await _taskFunctions.UpdateTaskAsync(taskId, status: status);

        // Assert
        result.Should().Contain("updated");
        result.Should().Contain(status);
    }

    [Theory]
    [InlineData("Low")]
    [InlineData("Medium")]
    [InlineData("High")]
    public async Task UpdateTaskAsync_WithAllValidPriorities_Succeeds(string priority)
    {
        // Arrange
        const int taskId = 1;
        var task = TaskItem.Create("Test Task", "Description", TaskPriority.Medium, TestUserId);
        
        _mockRepository.GetByIdAsync(taskId, TestUserId).Returns(task);

        // Act
        string result = await _taskFunctions.UpdateTaskAsync(taskId, priority: priority);

        // Assert
        result.Should().Contain("updated");
        result.Should().Contain(priority);
    }

    [Fact]
    public async Task UpdateTaskAsync_WithCaseInsensitiveStatus_Succeeds()
    {
        // Arrange
        const int taskId = 1;
        var task = TaskItem.Create("Test Task", "Description", TaskPriority.Medium, TestUserId);
        
        _mockRepository.GetByIdAsync(taskId, TestUserId).Returns(task);

        // Act
        string result = await _taskFunctions.UpdateTaskAsync(taskId, status: "inprogress"); // lowercase

        // Assert
        result.Should().Contain("updated");
    }

    [Fact]
    public async Task UpdateTaskAsync_WithCaseInsensitivePriority_Succeeds()
    {
        // Arrange
        const int taskId = 1;
        var task = TaskItem.Create("Test Task", "Description", TaskPriority.Low, TestUserId);
        
        _mockRepository.GetByIdAsync(taskId, TestUserId).Returns(task);

        // Act
        string result = await _taskFunctions.UpdateTaskAsync(taskId, priority: "HIGH"); // uppercase

        // Assert
        result.Should().Contain("updated");
    }

    #endregion

    #region Validation Error Tests

    [Fact]
    public async Task UpdateTaskAsync_WithNoFieldsToUpdate_ReturnsErrorMessage()
    {
        // Arrange
        const int taskId = 1;

        // Act
        string result = await _taskFunctions.UpdateTaskAsync(taskId);

        // Assert
        result.Should().Contain(ErrorMessages.UPDATE_REQUIRES_FIELDS);
        await _mockRepository.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<string>());
    }

    [Fact]
    public async Task UpdateTaskAsync_WithNullStatusAndNullPriority_ReturnsErrorMessage()
    {
        // Arrange
        const int taskId = 1;

        // Act
        string result = await _taskFunctions.UpdateTaskAsync(taskId, status: null, priority: null);

        // Assert
        result.Should().Contain(ErrorMessages.UPDATE_REQUIRES_FIELDS);
    }

    [Fact]
    public async Task UpdateTaskAsync_WithInvalidStatus_ReturnsErrorMessage()
    {
        // Arrange
        const int taskId = 1;
        var task = TaskItem.Create("Test Task", "Description", TaskPriority.Medium, TestUserId);
        
        _mockRepository.GetByIdAsync(taskId, TestUserId).Returns(task);

        // Act
        string result = await _taskFunctions.UpdateTaskAsync(taskId, status: "InvalidStatus");

        // Assert
        result.Should().Contain("InvalidStatus");
        result.Should().Contain("Invalid");
    }

    [Fact]
    public async Task UpdateTaskAsync_WithInvalidPriority_ReturnsErrorMessage()
    {
        // Arrange
        const int taskId = 1;
        var task = TaskItem.Create("Test Task", "Description", TaskPriority.Medium, TestUserId);
        
        _mockRepository.GetByIdAsync(taskId, TestUserId).Returns(task);

        // Act
        string result = await _taskFunctions.UpdateTaskAsync(taskId, priority: "InvalidPriority");

        // Assert
        result.Should().Contain("InvalidPriority");
        result.Should().Contain("Invalid");
    }

    #endregion

    #region Not Found Tests

    [Fact]
    public async Task UpdateTaskAsync_WithNonExistentTask_ReturnsNotFoundMessage()
    {
        // Arrange
        const int nonExistentTaskId = 999;
        
        _mockRepository.GetByIdAsync(nonExistentTaskId, TestUserId).Returns((TaskItem?)null);

        // Act
        string result = await _taskFunctions.UpdateTaskAsync(nonExistentTaskId, status: "InProgress");

        // Assert
        result.Should().Contain("not found");
        result.Should().Contain(nonExistentTaskId.ToString());
        await _mockRepository.DidNotReceive().UpdateAsync(Arg.Any<TaskItem>());
    }

    #endregion

    #region Business Rule Violation Tests

    [Fact]
    public async Task UpdateTaskAsync_CompletedToPending_ReturnsBusinessRuleError()
    {
        // Arrange
        const int taskId = 1;
        var task = TaskItem.Create("Test Task", "Description", TaskPriority.Medium, TestUserId);
        task.UpdateStatus(DomainTaskStatus.Completed); // Set to completed first
        
        _mockRepository.GetByIdAsync(taskId, TestUserId).Returns(task);

        // Act
        string result = await _taskFunctions.UpdateTaskAsync(taskId, status: "Pending");

        // Assert
        result.Should().Contain(ErrorMessages.BUSINESS_RULE_ERROR_PREFIX);
        await _mockRepository.DidNotReceive().UpdateAsync(Arg.Any<TaskItem>());
    }

    #endregion

    #region Repository Interaction Tests

    [Fact]
    public async Task UpdateTaskAsync_CallsRepositoryInCorrectOrder()
    {
        // Arrange
        const int taskId = 1;
        var task = TaskItem.Create("Test Task", "Description", TaskPriority.Medium, TestUserId);
        
        _mockRepository.GetByIdAsync(taskId, TestUserId).Returns(task);

        // Act
        await _taskFunctions.UpdateTaskAsync(taskId, status: "InProgress");

        // Assert
        Received.InOrder(() =>
        {
            _mockRepository.GetByIdAsync(taskId, TestUserId);
            _mockRepository.UpdateAsync(task);
            _mockRepository.SaveChangesAsync();
        });
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
