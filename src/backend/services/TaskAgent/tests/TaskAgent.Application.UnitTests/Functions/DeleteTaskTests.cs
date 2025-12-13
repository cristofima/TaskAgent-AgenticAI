using Microsoft.Extensions.Logging.Abstractions;
using TaskAgent.Application.Constants;
using TaskAgent.Application.Functions;
using TaskAgent.Application.Interfaces;
using TaskAgent.Application.Telemetry;
using TaskAgent.Domain.Entities;
using TaskAgent.Domain.Enums;

namespace TaskAgent.Application.UnitTests.Functions;

/// <summary>
/// Unit tests for TaskFunctions.DeleteTaskAsync following AAA pattern
/// </summary>
public class DeleteTaskTests
{
    private const string TestUserId = "test-user-id-12345";
    private readonly ITaskRepository _mockRepository;
    private readonly IUserContext _mockUserContext;
    private readonly TaskFunctions _taskFunctions;

    public DeleteTaskTests()
    {
        _mockRepository = Substitute.For<ITaskRepository>();
        _mockUserContext = Substitute.For<IUserContext>();
        _mockUserContext.UserId.Returns(TestUserId);
        _mockUserContext.IsAuthenticated.Returns(true);
        _taskFunctions = CreateTaskFunctions(_mockRepository, _mockUserContext);
    }

    #region Happy Path Tests

    [Fact]
    public async Task DeleteTaskAsync_WithValidId_DeletesTask()
    {
        // Arrange
        const int taskId = 1;
        const string taskTitle = "Test Task";
        var task = TaskItem.Create(taskTitle, "Description", TaskPriority.Medium, TestUserId);
        
        _mockRepository.GetByIdAsync(taskId, TestUserId).Returns(task);

        // Act
        string result = await _taskFunctions.DeleteTaskAsync(taskId);

        // Assert
        result.Should().Contain(SuccessMessages.TASK_DELETED_SUCCESS.Replace("{0}", taskId.ToString()).Split("'")[0]);
        result.Should().Contain(taskTitle);
        await _mockRepository.Received(1).DeleteAsync(taskId, TestUserId);
        await _mockRepository.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task DeleteTaskAsync_WithValidId_CallsRepositoryDeleteAsync()
    {
        // Arrange
        const int taskId = 42;
        var task = TaskItem.Create("Task to Delete", "Description", TaskPriority.Low, TestUserId);
        
        _mockRepository.GetByIdAsync(taskId, TestUserId).Returns(task);

        // Act
        await _taskFunctions.DeleteTaskAsync(taskId);

        // Assert
        await _mockRepository.Received(1).DeleteAsync(taskId, TestUserId);
    }

    [Fact]
    public async Task DeleteTaskAsync_WithValidId_SavesChanges()
    {
        // Arrange
        const int taskId = 1;
        var task = TaskItem.Create("Test Task", "Description", TaskPriority.Medium, TestUserId);
        
        _mockRepository.GetByIdAsync(taskId, TestUserId).Returns(task);

        // Act
        await _taskFunctions.DeleteTaskAsync(taskId);

        // Assert
        await _mockRepository.Received(1).SaveChangesAsync();
    }

    #endregion

    #region Not Found Tests

    [Fact]
    public async Task DeleteTaskAsync_WithNonExistentId_ReturnsNotFoundMessage()
    {
        // Arrange
        const int nonExistentId = 999;
        
        _mockRepository.GetByIdAsync(nonExistentId, TestUserId).Returns((TaskItem?)null);

        // Act
        string result = await _taskFunctions.DeleteTaskAsync(nonExistentId);

        // Assert
        result.Should().Contain("not found");
        result.Should().Contain(nonExistentId.ToString());
    }

    [Fact]
    public async Task DeleteTaskAsync_WithNonExistentId_DoesNotCallDeleteAsync()
    {
        // Arrange
        const int nonExistentId = 999;
        
        _mockRepository.GetByIdAsync(nonExistentId, TestUserId).Returns((TaskItem?)null);

        // Act
        await _taskFunctions.DeleteTaskAsync(nonExistentId);

        // Assert
        await _mockRepository.DidNotReceive().DeleteAsync(Arg.Any<int>(), Arg.Any<string>());
        await _mockRepository.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task DeleteTaskAsync_WithZeroId_ReturnsNotFoundMessage()
    {
        // Arrange
        const int zeroId = 0;
        
        _mockRepository.GetByIdAsync(zeroId, TestUserId).Returns((TaskItem?)null);

        // Act
        string result = await _taskFunctions.DeleteTaskAsync(zeroId);

        // Assert
        result.Should().Contain("not found");
    }

    [Fact]
    public async Task DeleteTaskAsync_WithNegativeId_ReturnsNotFoundMessage()
    {
        // Arrange
        const int negativeId = -1;
        
        _mockRepository.GetByIdAsync(negativeId, TestUserId).Returns((TaskItem?)null);

        // Act
        string result = await _taskFunctions.DeleteTaskAsync(negativeId);

        // Assert
        result.Should().Contain("not found");
    }

    #endregion

    #region Success Message Format Tests

    [Fact]
    public async Task DeleteTaskAsync_ReturnsSuccessMessageWithTaskId()
    {
        // Arrange
        const int taskId = 123;
        var task = TaskItem.Create("My Important Task", "Description", TaskPriority.High, TestUserId);
        
        _mockRepository.GetByIdAsync(taskId, TestUserId).Returns(task);

        // Act
        string result = await _taskFunctions.DeleteTaskAsync(taskId);

        // Assert
        result.Should().Contain(taskId.ToString());
    }

    [Fact]
    public async Task DeleteTaskAsync_ReturnsSuccessMessageWithTaskTitle()
    {
        // Arrange
        const int taskId = 1;
        const string taskTitle = "My Special Task Title";
        var task = TaskItem.Create(taskTitle, "Description", TaskPriority.Medium, TestUserId);
        
        _mockRepository.GetByIdAsync(taskId, TestUserId).Returns(task);

        // Act
        string result = await _taskFunctions.DeleteTaskAsync(taskId);

        // Assert
        result.Should().Contain(taskTitle);
    }

    #endregion

    #region Repository Interaction Tests

    [Fact]
    public async Task DeleteTaskAsync_CallsRepositoryInCorrectOrder()
    {
        // Arrange
        const int taskId = 1;
        var task = TaskItem.Create("Test Task", "Description", TaskPriority.Medium, TestUserId);
        
        _mockRepository.GetByIdAsync(taskId, TestUserId).Returns(task);

        // Act
        await _taskFunctions.DeleteTaskAsync(taskId);

        // Assert
        Received.InOrder(() =>
        {
            _mockRepository.GetByIdAsync(taskId, TestUserId);
            _mockRepository.DeleteAsync(taskId, TestUserId);
            _mockRepository.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task DeleteTaskAsync_WithValidId_FirstChecksIfTaskExists()
    {
        // Arrange
        const int taskId = 1;
        var task = TaskItem.Create("Test Task", "Description", TaskPriority.Medium, TestUserId);
        
        _mockRepository.GetByIdAsync(taskId, TestUserId).Returns(task);

        // Act
        await _taskFunctions.DeleteTaskAsync(taskId);

        // Assert
        await _mockRepository.Received(1).GetByIdAsync(taskId, TestUserId);
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
