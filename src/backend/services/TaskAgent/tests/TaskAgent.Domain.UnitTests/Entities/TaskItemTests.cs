using TaskAgent.Domain.Constants;
using TaskAgent.Domain.Entities;
using TaskAgent.Domain.Enums;
using DomainTaskStatus = TaskAgent.Domain.Enums.TaskStatus;

namespace TaskAgent.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for TaskItem entity following AAA pattern (Arrange-Act-Assert)
/// </summary>
public class TaskItemTests
{
    #region Factory Method Tests - Create()

    [Fact]
    public void Create_WithValidData_ReturnsTaskItem()
    {
        // Arrange
        const string title = "Test Task";
        const string description = "Test Description";
        const TaskPriority priority = TaskPriority.Medium;

        // Act
        var task = TaskItem.Create(title, description, priority);

        // Assert
        task.Should().NotBeNull();
        task.Title.Should().Be(title);
        task.Description.Should().Be(description);
        task.Priority.Should().Be(priority);
    }

    [Fact]
    public void Create_WithEmptyTitle_ThrowsArgumentException()
    {
        // Arrange
        const string emptyTitle = "";
        const string description = "Test Description";
        const TaskPriority priority = TaskPriority.Medium;

        // Act
        Action act = () => TaskItem.Create(emptyTitle, description, priority);

        // Assert
        act.Should().Throw<ArgumentException>()
           .WithMessage($"*{ValidationMessages.TITLE_REQUIRED}*");
    }

    [Fact]
    public void Create_WithWhitespaceTitle_ThrowsArgumentException()
    {
        // Arrange
        const string whitespaceTitle = "   ";
        const string description = "Test Description";
        const TaskPriority priority = TaskPriority.Medium;

        // Act
        Action act = () => TaskItem.Create(whitespaceTitle, description, priority);

        // Assert
        act.Should().Throw<ArgumentException>()
           .WithMessage($"*{ValidationMessages.TITLE_REQUIRED}*");
    }

    [Fact]
    public void Create_WithTitleExceedingMaxLength_ThrowsArgumentException()
    {
        // Arrange
        string longTitle = new string('A', TaskConstants.MAX_TITLE_LENGTH + 1);
        const string description = "Test Description";
        const TaskPriority priority = TaskPriority.Medium;

        // Act
        Action act = () => TaskItem.Create(longTitle, description, priority);

        // Assert
        act.Should().Throw<ArgumentException>()
           .WithMessage($"*{ValidationMessages.TITLE_TOO_LONG}*");
    }

    [Fact]
    public void Create_WithTitleAtMaxLength_Succeeds()
    {
        // Arrange
        string maxLengthTitle = new string('A', TaskConstants.MAX_TITLE_LENGTH);
        const string description = "Test Description";
        const TaskPriority priority = TaskPriority.Medium;

        // Act
        var task = TaskItem.Create(maxLengthTitle, description, priority);

        // Assert
        task.Title.Should().HaveLength(TaskConstants.MAX_TITLE_LENGTH);
    }

    [Fact]
    public void Create_WithNullDescription_UsesEmptyString()
    {
        // Arrange
        const string title = "Test Task";
        const string? nullDescription = null;
        const TaskPriority priority = TaskPriority.Medium;

        // Act
        var task = TaskItem.Create(title, nullDescription!, priority);

        // Assert
        task.Description.Should().BeEmpty();
    }

    [Fact]
    public void Create_SetsDefaultStatusToPending()
    {
        // Arrange
        const string title = "Test Task";
        const string description = "Test Description";
        const TaskPriority priority = TaskPriority.High;

        // Act
        var task = TaskItem.Create(title, description, priority);

        // Assert
        task.Status.Should().Be(DomainTaskStatus.Pending);
    }

    [Fact]
    public void Create_SetsCreatedAtToUtcNow()
    {
        // Arrange
        const string title = "Test Task";
        const string description = "Test Description";
        const TaskPriority priority = TaskPriority.Low;
        DateTime beforeCreation = DateTime.UtcNow;

        // Act
        var task = TaskItem.Create(title, description, priority);

        // Assert
        DateTime afterCreation = DateTime.UtcNow;
        task.CreatedAt.Should().BeOnOrAfter(beforeCreation);
        task.CreatedAt.Should().BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void Create_SetsUpdatedAtToUtcNow()
    {
        // Arrange
        const string title = "Test Task";
        const string description = "Test Description";
        const TaskPriority priority = TaskPriority.Medium;
        DateTime beforeCreation = DateTime.UtcNow;

        // Act
        var task = TaskItem.Create(title, description, priority);

        // Assert
        DateTime afterCreation = DateTime.UtcNow;
        task.UpdatedAt.Should().BeOnOrAfter(beforeCreation);
        task.UpdatedAt.Should().BeOnOrBefore(afterCreation);
    }

    #endregion

    #region UpdateStatus Tests

    [Fact]
    public void UpdateStatus_FromPendingToInProgress_UpdatesStatus()
    {
        // Arrange
        TaskItem task = CreateTestTask();
        task.Status.Should().Be(DomainTaskStatus.Pending);

        // Act
        task.UpdateStatus(DomainTaskStatus.InProgress);

        // Assert
        task.Status.Should().Be(DomainTaskStatus.InProgress);
    }

    [Fact]
    public void UpdateStatus_FromInProgressToCompleted_UpdatesStatus()
    {
        // Arrange
        TaskItem task = CreateTestTask();
        task.UpdateStatus(DomainTaskStatus.InProgress);

        // Act
        task.UpdateStatus(DomainTaskStatus.Completed);

        // Assert
        task.Status.Should().Be(DomainTaskStatus.Completed);
    }

    [Fact]
    public void UpdateStatus_FromCompletedToPending_ThrowsInvalidOperationException()
    {
        // Arrange
        TaskItem task = CreateTestTask();
        task.UpdateStatus(DomainTaskStatus.InProgress);
        task.UpdateStatus(DomainTaskStatus.Completed);

        // Act
        Action act = () => task.UpdateStatus(DomainTaskStatus.Pending);

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage(ValidationMessages.CANNOT_REOPEN_COMPLETED_TASK);
    }

    [Fact]
    public void UpdateStatus_FromCompletedToInProgress_Succeeds()
    {
        // Arrange
        TaskItem task = CreateTestTask();
        task.UpdateStatus(DomainTaskStatus.InProgress);
        task.UpdateStatus(DomainTaskStatus.Completed);

        // Act
        task.UpdateStatus(DomainTaskStatus.InProgress);

        // Assert
        task.Status.Should().Be(DomainTaskStatus.InProgress);
    }

    [Fact]
    public void UpdateStatus_SameStatus_DoesNotChangeUpdatedAt()
    {
        // Arrange
        TaskItem task = CreateTestTask();
        DateTime originalUpdatedAt = task.UpdatedAt;

        // Act
        task.UpdateStatus(DomainTaskStatus.Pending);

        // Assert
        task.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Fact]
    public void UpdateStatus_DifferentStatus_UpdatesUpdatedAtTimestamp()
    {
        // Arrange
        TaskItem task = CreateTestTask();
        DateTime originalUpdatedAt = task.UpdatedAt;

        // Small delay to ensure timestamp difference
        Thread.Sleep(10);

        // Act
        task.UpdateStatus(DomainTaskStatus.InProgress);

        // Assert
        task.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    #endregion

    #region UpdatePriority Tests

    [Theory]
    [InlineData(TaskPriority.Low)]
    [InlineData(TaskPriority.Medium)]
    [InlineData(TaskPriority.High)]
    public void UpdatePriority_WithDifferentPriority_UpdatesPriority(TaskPriority newPriority)
    {
        // Arrange
        TaskItem task = CreateTestTask(priority: TaskPriority.Medium);
        if (newPriority == TaskPriority.Medium)
        {
            task = CreateTestTask(priority: TaskPriority.Low);
        }

        // Act
        task.UpdatePriority(newPriority);

        // Assert
        task.Priority.Should().Be(newPriority);
    }

    [Fact]
    public void UpdatePriority_SamePriority_DoesNotChangeUpdatedAt()
    {
        // Arrange
        TaskItem task = CreateTestTask(priority: TaskPriority.Medium);
        DateTime originalUpdatedAt = task.UpdatedAt;

        // Act
        task.UpdatePriority(TaskPriority.Medium);

        // Assert
        task.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Fact]
    public void UpdatePriority_DifferentPriority_UpdatesUpdatedAtTimestamp()
    {
        // Arrange
        TaskItem task = CreateTestTask(priority: TaskPriority.Low);
        DateTime originalUpdatedAt = task.UpdatedAt;

        // Small delay to ensure timestamp difference
        Thread.Sleep(10);

        // Act
        task.UpdatePriority(TaskPriority.High);

        // Assert
        task.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    #endregion

    #region IsHighPriority Tests

    [Fact]
    public void IsHighPriority_WhenHigh_ReturnsTrue()
    {
        // Arrange
        TaskItem task = CreateTestTask(priority: TaskPriority.High);

        // Act
        bool result = task.IsHighPriority();

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(TaskPriority.Low)]
    [InlineData(TaskPriority.Medium)]
    public void IsHighPriority_WhenNotHigh_ReturnsFalse(TaskPriority priority)
    {
        // Arrange
        TaskItem task = CreateTestTask(priority: priority);

        // Act
        bool result = task.IsHighPriority();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Factory helper method for creating test TaskItem instances
    /// Follows Microsoft best practice: use helper methods instead of Setup/Teardown
    /// </summary>
    private static TaskItem CreateTestTask(
        string title = "Test Task",
        string description = "Test Description",
        TaskPriority priority = TaskPriority.Medium) =>
        TaskItem.Create(title, description, priority);

    #endregion
}
