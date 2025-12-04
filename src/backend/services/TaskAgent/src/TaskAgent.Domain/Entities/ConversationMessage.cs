namespace TaskAgent.Domain.Entities;

/// <summary>
/// Represents an individual conversation message stored in PostgreSQL.
/// </summary>
public class ConversationMessage
{
    public string MessageId { get; private set; } = string.Empty;
    public string ThreadId { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTimeOffset Timestamp { get; private set; }

    private ConversationMessage() { }

    /// <summary>
    /// Creates a new conversation message.
    /// </summary>
    /// <param name="messageId">Unique message identifier (GUID format).</param>
    /// <param name="threadId">Thread identifier.</param>
    /// <param name="role">Message role (User, Assistant, Tool, System).</param>
    /// <param name="content">Serialized message content (JSON array of AIContent).</param>
    /// <returns>New <see cref="ConversationMessage"/> instance.</returns>
    public static ConversationMessage Create(
        string messageId,
        string threadId,
        string role,
        string content
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(threadId);
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        return new ConversationMessage
        {
            MessageId = messageId,
            ThreadId = threadId,
            Role = role,
            Content = content,
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}
