namespace TaskAgent.Domain.Entities;

/// <summary>
/// Stores conversation thread metadata including serialized state for AG-UI protocol.
/// </summary>
public class ConversationThreadMetadata
{
    public string ThreadId { get; private set; } = string.Empty;
    public string? Title { get; private set; }
    public string? Preview { get; private set; }
    public int MessageCount { get; private set; }
    public string? SerializedState { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private ConversationThreadMetadata() { }

    /// <summary>
    /// Creates new conversation thread metadata.
    /// </summary>
    public static ConversationThreadMetadata Create(
        string threadId,
        string? serializedState = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(threadId);

        return new ConversationThreadMetadata
        {
            ThreadId = threadId,
            SerializedState = serializedState,
            MessageCount = 0,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Updates thread metadata after new messages.
    /// </summary>
    public void UpdateMetadata(
        string? title,
        string? preview,
        int messageCount,
        string? serializedState
    )
    {
        Title = title;
        Preview = preview;
        MessageCount = messageCount;
        SerializedState = serializedState;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates serialized state for AG-UI protocol continuity.
    /// </summary>
    public void UpdateSerializedState(string? serializedState)
    {
        SerializedState = serializedState;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Soft deletes the thread.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
