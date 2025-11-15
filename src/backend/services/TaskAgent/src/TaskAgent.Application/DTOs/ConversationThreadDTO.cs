namespace TaskAgent.Application.DTOs;

/// <summary>
/// Represents a conversation thread with metadata
/// </summary>
public record ConversationThreadDTO
{
    /// <summary>
    /// Unique identifier for the thread
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Title or preview of the conversation
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Preview of the first or last message
    /// </summary>
    public string? Preview { get; init; }

    /// <summary>
    /// When the thread was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When the thread was last updated
    /// </summary>
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Number of messages in the thread
    /// </summary>
    public int MessageCount { get; init; }

    /// <summary>
    /// Whether the thread is active
    /// </summary>
    public bool IsActive { get; init; } = true;
}
