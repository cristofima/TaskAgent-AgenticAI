using Microsoft.Extensions.Logging.Abstractions;
using TaskAgent.Application.Functions;
using TaskAgent.Application.Interfaces;
using TaskAgent.Application.Telemetry;
using TaskAgent.Domain.Entities;
using TaskAgent.Domain.Enums;

namespace TaskAgent.Application.UnitTests.Functions;

/// <summary>
/// Unit tests for TaskFunctions.GetTaskDetailsAsync following AAA pattern
/// </summary>
public class GetTaskDetailsTests
{
    private readonly ITaskRepository _mockRepository;
    private readonly TaskFunctions _taskFunctions;

    public GetTaskDetailsTests()
    {
        _mockRepository = Substitute.For<ITaskRepository>();
        _taskFunctions = CreateTaskFunctions(_mockRepository);
    }

    #region Happy Path Tests

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
        result.Should().Contain("Test Description");
        result.Should().Contain("High");
        result.Should().Contain("Pending");
    }

    [Fact]
    public async Task GetTaskDetailsAsync_WithValidId_CallsRepository()
    {
        // Arrange
        const int taskId = 42;
        var task = TaskItem.Create("Test Task", "Test Description", TaskPriority.Medium);
        
        _mockRepository.GetByIdAsync(taskId).Returns(task);

        // Act
        await _taskFunctions.GetTaskDetailsAsync(taskId);

        // Assert
        await _mockRepository.Received(1).GetByIdAsync(taskId);
    }

    #endregion

    #region Not Found Tests

    [Fact]
    public async Task GetTaskDetailsAsync_WithInvalidId_ReturnsNotFoundMessage()
    {
        // Arrange
        const int invalidTaskId = 999;
        
        _mockRepository.GetByIdAsync(invalidTaskId).Returns((TaskItem?)null);

        // Act
        string result = await _taskFunctions.GetTaskDetailsAsync(invalidTaskId);

        // Assert
        result.Should().Contain("not found");
        result.Should().Contain(invalidTaskId.ToString());
    }

    [Fact]
    public async Task GetTaskDetailsAsync_WithZeroId_ReturnsNotFoundMessage()
    {
        // Arrange
        const int zeroId = 0;
        
        _mockRepository.GetByIdAsync(zeroId).Returns((TaskItem?)null);

        // Act
        string result = await _taskFunctions.GetTaskDetailsAsync(zeroId);

        // Assert
        result.Should().Contain("not found");
    }

    [Fact]
    public async Task GetTaskDetailsAsync_WithNegativeId_ReturnsNotFoundMessage()
    {
        // Arrange
        const int negativeId = -1;
        
        _mockRepository.GetByIdAsync(negativeId).Returns((TaskItem?)null);

        // Act
        string result = await _taskFunctions.GetTaskDetailsAsync(negativeId);

        // Assert
        result.Should().Contain("not found");
    }

    #endregion

    #region Format Tests

    [Fact]
    public async Task GetTaskDetailsAsync_DisplaysAllTaskFields()
    {
        // Arrange
        const int taskId = 1;
        var task = TaskItem.Create("My Task", "My Description", TaskPriority.Low);
        
        _mockRepository.GetByIdAsync(taskId).Returns(task);

        // Act
        string result = await _taskFunctions.GetTaskDetailsAsync(taskId);

        // Assert
        result.Should().Contain("ID:");
        result.Should().Contain("Title:");
        result.Should().Contain("Description:");
        result.Should().Contain("Priority:");
        result.Should().Contain("Status:");
        result.Should().Contain("Created:");
        result.Should().Contain("Last Updated:");
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
