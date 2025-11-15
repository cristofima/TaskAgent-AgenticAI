namespace TaskAgent.Application.DTOs;

/// <summary>
/// Represents a single chat message in a conversation
/// </summary>
public abstract record ChatMessage
{
    /// <summary>
    /// Unique identifier for the message
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The message content
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Role of the message sender (user or assistant)
    /// </summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp when the message was created
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Metadata associated with this message
    /// </summary>
    public MessageMetadata? Metadata { get; init; }
}

/// <summary>
/// Concrete implementation of ChatMessage for conversation history
/// </summary>
public sealed record ConversationMessage : ChatMessage
{
    /// <summary>
    /// Creates a new conversation message
    /// </summary>
    public static ConversationMessage Create(
        string role,
        string content,
        DateTime? createdAt = null,
        MessageMetadata? metadata = null
    )
    {
        return new ConversationMessage
        {
            Id = Guid.NewGuid().ToString(),
            Role = role,
            Content = content,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            Metadata = metadata,
        };
    }
}
