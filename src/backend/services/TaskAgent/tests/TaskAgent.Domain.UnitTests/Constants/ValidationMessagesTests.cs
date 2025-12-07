using TaskAgent.Domain.Constants;

namespace TaskAgent.Domain.UnitTests.Constants;

/// <summary>
/// Unit tests for ValidationMessages to ensure messages are defined correctly
/// </summary>
public class ValidationMessagesTests
{
    [Fact]
    public void TitleRequired_ShouldNotBeNullOrEmpty()
    {
        // Arrange & Act
        string message = ValidationMessages.TITLE_REQUIRED;

        // Assert
        message.Should().NotBeNullOrWhiteSpace();
        message.Should().Contain("Title");
    }

    [Fact]
    public void TitleTooLong_ShouldNotBeNullOrEmpty()
    {
        // Arrange & Act
        string message = ValidationMessages.TITLE_TOO_LONG;

        // Assert
        message.Should().NotBeNullOrWhiteSpace();
        message.Should().Contain("Title");
    }

    [Fact]
    public void CannotReopenCompletedTask_ShouldNotBeNullOrEmpty()
    {
        // Arrange & Act
        string message = ValidationMessages.CANNOT_REOPEN_COMPLETED_TASK;

        // Assert
        message.Should().NotBeNullOrWhiteSpace();
        message.Should().Contain("completed");
    }
}
