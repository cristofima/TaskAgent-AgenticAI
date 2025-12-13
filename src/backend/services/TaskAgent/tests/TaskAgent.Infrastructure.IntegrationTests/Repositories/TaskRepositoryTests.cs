using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskAgent.Domain.Entities;
using TaskAgent.Domain.Enums;
using TaskAgent.Infrastructure.Data;
using TaskAgent.Infrastructure.IntegrationTests.Fixtures;
using TaskAgent.Infrastructure.Repositories;
using DomainTaskStatus = TaskAgent.Domain.Enums.TaskStatus;

namespace TaskAgent.Infrastructure.IntegrationTests.Repositories;

/// <summary>
/// Integration tests for TaskRepository using SQL Server Testcontainers.
/// These tests verify actual database operations against a real SQL Server instance.
/// </summary>
[Collection("SqlServer")]
public class TaskRepositoryTests : IAsyncLifetime
{
    private readonly SqlServerContainerFixture _fixture;
    private TaskDbContext _context = null!;
    private TaskRepository _repository = null!;
    
    private const string TestUserId = "test-user-id-12345";
    private const string AnotherUserId = "another-user-id-67890"; // For testing user isolation

    public TaskRepositoryTests(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        // Create fresh context for each test to ensure isolation
        _context = _fixture.CreateDbContext();
        _repository = new TaskRepository(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        // Clean up test data after each test
        await _context.Tasks.ExecuteDeleteAsync();
        await _context.DisposeAsync();
    }

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WithValidTask_InsertsTaskToDatabase()
    {
        // Arrange
        var task = TaskItem.Create("Integration Test Task", "Test description", TaskPriority.High, TestUserId);

        // Act
        var result = await _repository.AddAsync(task);
        await _repository.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);

        // Verify in database with a new context
        await using TaskDbContext verificationContext = _fixture.CreateDbContext();
        TaskItem? savedTask = await verificationContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == result.Id);

        savedTask.Should().NotBeNull();
        savedTask!.Title.Should().Be("Integration Test Task");
        savedTask.Description.Should().Be("Test description");
        savedTask.Priority.Should().Be(TaskPriority.High);
        savedTask.Status.Should().Be(DomainTaskStatus.Pending);
    }

    [Fact]
    public async Task AddAsync_WithNullTask_ThrowsArgumentNullException()
    {
        // Arrange
        TaskItem? task = null;

        // Act
        Func<Task> act = async () => await _repository.AddAsync(task!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("task");
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenTaskExists_ReturnsTask()
    {
        // Arrange
        var task = TaskItem.Create("Get By Id Test", "Description", TaskPriority.Medium, TestUserId);
        await _repository.AddAsync(task);
        await _repository.SaveChangesAsync();

        // Act
        TaskItem? result = await _repository.GetByIdAsync(task.Id, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(task.Id);
        result.Title.Should().Be("Get By Id Test");
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaskDoesNotExist_ReturnsNull()
    {
        // Arrange
        const int nonExistentId = 99999;

        // Act
        TaskItem? result = await _repository.GetByIdAsync(nonExistentId, TestUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaskBelongsToDifferentUser_ReturnsNull()
    {
        // Arrange - Create task for a different user
        var task = TaskItem.Create("Other User Task", "Description", TaskPriority.Medium, AnotherUserId);
        await _repository.AddAsync(task);
        await _repository.SaveChangesAsync();

        // Act - Try to get task with TestUserId
        TaskItem? result = await _repository.GetByIdAsync(task.Id, TestUserId);

        // Assert - Should not find the task (user isolation)
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithMultipleTasks_ReturnsAllTasksOrderedByCreatedAtDesc()
    {
        // Arrange
        var task1 = TaskItem.Create("First Task", "Description 1", TaskPriority.Low, TestUserId);
        await _repository.AddAsync(task1);
        await _repository.SaveChangesAsync();

        // Small delay to ensure different CreatedAt timestamps
        await Task.Delay(10);

        var task2 = TaskItem.Create("Second Task", "Description 2", TaskPriority.Medium, TestUserId);
        await _repository.AddAsync(task2);
        await _repository.SaveChangesAsync();

        await Task.Delay(10);

        var task3 = TaskItem.Create("Third Task", "Description 3", TaskPriority.High, TestUserId);
        await _repository.AddAsync(task3);
        await _repository.SaveChangesAsync();

        // Act
        IEnumerable<TaskItem> result = await _repository.GetAllByUserAsync(TestUserId);
        var taskList = result.ToList();

        // Assert
        taskList.Should().HaveCount(3);
        taskList[0].Title.Should().Be("Third Task"); // Most recent first
        taskList[1].Title.Should().Be("Second Task");
        taskList[2].Title.Should().Be("First Task");
    }

    [Fact]
    public async Task GetAllAsync_WhenNoTasks_ReturnsEmptyCollection()
    {
        // Arrange - database is already empty due to cleanup in InitializeAsync

        // Act
        IEnumerable<TaskItem> result = await _repository.GetAllByUserAsync(TestUserId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_WithStatusFilter_ReturnsFilteredTasks()
    {
        // Arrange
        var pendingTask = TaskItem.Create("Pending Task", "Description", TaskPriority.Low, TestUserId);
        var inProgressTask = TaskItem.Create("In Progress Task", "Description", TaskPriority.Medium, TestUserId);
        inProgressTask.UpdateStatus(DomainTaskStatus.InProgress);

        await _repository.AddAsync(pendingTask);
        await _repository.AddAsync(inProgressTask);
        await _repository.SaveChangesAsync();

        // Act
        IEnumerable<TaskItem> result = await _repository.SearchAsync(TestUserId, status: DomainTaskStatus.InProgress);
        var taskList = result.ToList();

        // Assert
        taskList.Should().HaveCount(1);
        taskList[0].Title.Should().Be("In Progress Task");
        taskList[0].Status.Should().Be(DomainTaskStatus.InProgress);
    }

    [Fact]
    public async Task SearchAsync_WithPriorityFilter_ReturnsFilteredTasks()
    {
        // Arrange
        var lowTask = TaskItem.Create("Low Priority Task", "Description", TaskPriority.Low, TestUserId);
        var highTask = TaskItem.Create("High Priority Task", "Description", TaskPriority.High, TestUserId);

        await _repository.AddAsync(lowTask);
        await _repository.AddAsync(highTask);
        await _repository.SaveChangesAsync();

        // Act
        IEnumerable<TaskItem> result = await _repository.SearchAsync(TestUserId, priority: TaskPriority.High);
        var taskList = result.ToList();

        // Assert
        taskList.Should().HaveCount(1);
        taskList[0].Title.Should().Be("High Priority Task");
        taskList[0].Priority.Should().Be(TaskPriority.High);
    }

    [Fact]
    public async Task SearchAsync_WithBothFilters_ReturnsCombinedFilter()
    {
        // Arrange
        var matchingTask = TaskItem.Create("Matching Task", "Description", TaskPriority.High, TestUserId);
        matchingTask.UpdateStatus(DomainTaskStatus.InProgress);

        var wrongStatusTask = TaskItem.Create("Wrong Status", "Description", TaskPriority.High, TestUserId);
        // Status is Pending by default

        var wrongPriorityTask = TaskItem.Create("Wrong Priority", "Description", TaskPriority.Low, TestUserId);
        wrongPriorityTask.UpdateStatus(DomainTaskStatus.InProgress);

        await _repository.AddAsync(matchingTask);
        await _repository.AddAsync(wrongStatusTask);
        await _repository.AddAsync(wrongPriorityTask);
        await _repository.SaveChangesAsync();

        // Act
        IEnumerable<TaskItem> result = await _repository.SearchAsync(
            TestUserId,
            status: DomainTaskStatus.InProgress,
            priority: TaskPriority.High);
        var taskList = result.ToList();

        // Assert
        taskList.Should().HaveCount(1);
        taskList[0].Title.Should().Be("Matching Task");
    }

    [Fact]
    public async Task SearchAsync_WithNoFilters_ReturnsAllTasks()
    {
        // Arrange
        var task1 = TaskItem.Create("Task 1", "Description", TaskPriority.Low, TestUserId);
        var task2 = TaskItem.Create("Task 2", "Description", TaskPriority.High, TestUserId);

        await _repository.AddAsync(task1);
        await _repository.AddAsync(task2);
        await _repository.SaveChangesAsync();

        // Act
        IEnumerable<TaskItem> result = await _repository.SearchAsync(TestUserId);
        var taskList = result.ToList();

        // Assert
        taskList.Should().HaveCount(2);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidTask_ModifiesTask()
    {
        // Arrange
        var task = TaskItem.Create("Original Title", "Original Description", TaskPriority.Low, TestUserId);
        await _repository.AddAsync(task);
        await _repository.SaveChangesAsync();

        // Modify the task
        task.UpdateStatus(DomainTaskStatus.InProgress);
        task.UpdatePriority(TaskPriority.High);

        // Act
        await _repository.UpdateAsync(task);
        await _repository.SaveChangesAsync();

        // Assert - Verify in database with a new context
        await using TaskDbContext verificationContext = _fixture.CreateDbContext();
        TaskItem? updatedTask = await verificationContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == task.Id);

        updatedTask.Should().NotBeNull();
        updatedTask!.Status.Should().Be(DomainTaskStatus.InProgress);
        updatedTask.Priority.Should().Be(TaskPriority.High);
    }

    [Fact]
    public async Task UpdateAsync_WithNullTask_ThrowsArgumentNullException()
    {
        // Arrange
        TaskItem? task = null;

        // Act
        Func<Task> act = () => _repository.UpdateAsync(task!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("task");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WhenTaskExists_RemovesTask()
    {
        // Arrange
        var task = TaskItem.Create("Task To Delete", "Description", TaskPriority.Medium, TestUserId);
        await _repository.AddAsync(task);
        await _repository.SaveChangesAsync();
        int taskId = task.Id;

        // Act
        await _repository.DeleteAsync(taskId, TestUserId);
        await _repository.SaveChangesAsync();

        // Assert - Verify in database with a new context
        await using TaskDbContext verificationContext = _fixture.CreateDbContext();
        TaskItem? deletedTask = await verificationContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId);

        deletedTask.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenTaskDoesNotExist_DoesNotThrow()
    {
        // Arrange
        const int nonExistentId = 99999;

        // Act
        Func<Task> act = async () =>
        {
            await _repository.DeleteAsync(nonExistentId, TestUserId);
            await _repository.SaveChangesAsync();
        };

        // Assert - Should not throw
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region SaveChangesAsync Tests

    [Fact]
    public async Task SaveChangesAsync_WithPendingChanges_ReturnsAffectedRowCount()
    {
        // Arrange
        var task = TaskItem.Create("Save Changes Test", "Description", TaskPriority.Low, TestUserId);
        await _repository.AddAsync(task);

        // Act
        int result = await _repository.SaveChangesAsync();

        // Assert
        result.Should().Be(1);
    }

    #endregion
}
