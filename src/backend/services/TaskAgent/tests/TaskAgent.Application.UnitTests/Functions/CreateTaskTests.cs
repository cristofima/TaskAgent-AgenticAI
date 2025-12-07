using Microsoft.Extensions.Logging.Abstractions;
using TaskAgent.Application.Constants;
using TaskAgent.Application.Functions;
using TaskAgent.Application.Interfaces;
using TaskAgent.Application.Telemetry;
using TaskAgent.Domain.Entities;
using TaskAgent.Domain.Enums;

namespace TaskAgent.Application.UnitTests.Functions;

/// <summary>
/// Unit tests for TaskFunctions.CreateTaskAsync following AAA pattern
/// </summary>
public class CreateTaskTests
{
    private readonly ITaskRepository _mockRepository;
    private readonly TaskFunctions _taskFunctions;

    public CreateTaskTests()
    {
        _mockRepository = Substitute.For<ITaskRepository>();
        _taskFunctions = CreateTaskFunctions(_mockRepository);
    }

    #region Happy Path Tests

    [Fact]
    public async Task CreateTaskAsync_WithValidData_ReturnsSuccessMessage()
    {
        // Arrange
        const string title = "Test Task";
        const string description = "Test Description";
        const string priority = "Medium";

        _mockRepository.AddAsync(Arg.Any<TaskItem>())
            .Returns(callInfo => callInfo.Arg<TaskItem>());
        _mockRepository.SaveChangesAsync().Returns(1);

        // Act
        string result = await _taskFunctions.CreateTaskAsync(title, description, priority);

        // Assert
        result.Should().Contain(SuccessMessages.TASK_CREATED_SUCCESS);
        result.Should().Contain(title);
        result.Should().Contain(description);
        result.Should().Contain("Medium");
    }

    [Fact]
    public async Task CreateTaskAsync_WithValidData_PersistsToRepository()
    {
        // Arrange
        const string title = "Test Task";
        const string description = "Test Description";
        const string priority = "High";

        _mockRepository.AddAsync(Arg.Any<TaskItem>())
            .Returns(callInfo => callInfo.Arg<TaskItem>());
        _mockRepository.SaveChangesAsync().Returns(1);

        // Act
        await _taskFunctions.CreateTaskAsync(title, description, priority);

        // Assert
        await _mockRepository.Received(1).AddAsync(Arg.Is<TaskItem>(t =>
            t.Title == title &&
            t.Description == description &&
            t.Priority == TaskPriority.High));
        await _mockRepository.Received(1).SaveChangesAsync();
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task CreateTaskAsync_WithEmptyTitle_ReturnsErrorMessage()
    {
        // Arrange
        const string emptyTitle = "";
        const string description = "Test Description";
        const string priority = "Medium";

        // Act
        string result = await _taskFunctions.CreateTaskAsync(emptyTitle, description, priority);

        // Assert
        result.Should().Be(ErrorMessages.TASK_TITLE_EMPTY);
    }

    [Fact]
    public async Task CreateTaskAsync_WithWhitespaceTitle_ReturnsErrorMessage()
    {
        // Arrange
        const string whitespaceTitle = "   ";
        const string description = "Test Description";
        const string priority = "Medium";

        // Act
        string result = await _taskFunctions.CreateTaskAsync(whitespaceTitle, description, priority);

        // Assert
        result.Should().Be(ErrorMessages.TASK_TITLE_EMPTY);
    }

    [Fact]
    public async Task CreateTaskAsync_WithInvalidPriority_ReturnsErrorMessage()
    {
        // Arrange
        const string title = "Test Task";
        const string description = "Test Description";
        const string invalidPriority = "VeryHigh";

        // Act
        string result = await _taskFunctions.CreateTaskAsync(title, description, invalidPriority);

        // Assert
        result.Should().Contain("Invalid priority");
        result.Should().Contain(invalidPriority);
    }

    [Fact]
    public async Task CreateTaskAsync_WithNullDescription_UsesEmptyString()
    {
        // Arrange
        const string title = "Test Task";
        string? nullDescription = null;
        const string priority = "Low";

        _mockRepository.AddAsync(Arg.Any<TaskItem>())
            .Returns(callInfo => callInfo.Arg<TaskItem>());
        _mockRepository.SaveChangesAsync().Returns(1);

        // Act
        string result = await _taskFunctions.CreateTaskAsync(title, nullDescription!, priority);

        // Assert
        result.Should().Contain(SuccessMessages.TASK_CREATED_SUCCESS);
        await _mockRepository.Received(1).AddAsync(Arg.Is<TaskItem>(t =>
            t.Description == string.Empty));
    }

    #endregion

    #region Priority Parsing Tests

    [Theory]
    [InlineData("Low", TaskPriority.Low)]
    [InlineData("low", TaskPriority.Low)]
    [InlineData("LOW", TaskPriority.Low)]
    [InlineData("Medium", TaskPriority.Medium)]
    [InlineData("medium", TaskPriority.Medium)]
    [InlineData("MEDIUM", TaskPriority.Medium)]
    [InlineData("High", TaskPriority.High)]
    [InlineData("high", TaskPriority.High)]
    [InlineData("HIGH", TaskPriority.High)]
    public async Task CreateTaskAsync_ParsesPriorityCaseInsensitively(string input, TaskPriority expected)
    {
        // Arrange
        const string title = "Test Task";
        const string description = "Test Description";

        _mockRepository.AddAsync(Arg.Any<TaskItem>())
            .Returns(callInfo => callInfo.Arg<TaskItem>());
        _mockRepository.SaveChangesAsync().Returns(1);

        // Act
        string result = await _taskFunctions.CreateTaskAsync(title, description, input);

        // Assert
        result.Should().Contain(SuccessMessages.TASK_CREATED_SUCCESS);
        await _mockRepository.Received(1).AddAsync(Arg.Is<TaskItem>(t =>
            t.Priority == expected));
    }

    #endregion

    #region Default Priority Test

    [Fact]
    public async Task CreateTaskAsync_WithDefaultPriority_UsesMedium()
    {
        // Arrange
        const string title = "Test Task";
        const string description = "Test Description";

        _mockRepository.AddAsync(Arg.Any<TaskItem>())
            .Returns(callInfo => callInfo.Arg<TaskItem>());
        _mockRepository.SaveChangesAsync().Returns(1);

        // Act
        string result = await _taskFunctions.CreateTaskAsync(title, description);

        // Assert
        result.Should().Contain(SuccessMessages.TASK_CREATED_SUCCESS);
        result.Should().Contain("Medium");
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
