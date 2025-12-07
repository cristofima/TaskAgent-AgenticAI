using TaskAgent.Domain.Constants;

namespace TaskAgent.Domain.UnitTests.Constants;

/// <summary>
/// Unit tests for TaskConstants to ensure constant values are correct
/// </summary>
public class TaskConstantsTests
{
    [Fact]
    public void MaxTitleLength_ShouldBe200()
    {
        // Arrange & Act
        const int maxLength = TaskConstants.MAX_TITLE_LENGTH;

        // Assert
        maxLength.Should().Be(200);
    }

    [Fact]
    public void MaxDescriptionLength_ShouldBe1000()
    {
        // Arrange & Act
        const int maxLength = TaskConstants.MAX_DESCRIPTION_LENGTH;

        // Assert
        maxLength.Should().Be(1000);
    }
}
