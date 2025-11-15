namespace TaskAgent.Domain.Entities;

/// <summary>
/// Domain entity for conversation thread persistence
/// Stores complete serialized AgentThread as JSON blob
/// </summary>
public class ConversationThread
{
    /// <summary>
    /// Unique thread identifier (GUID format)
    /// </summary>
    public string ThreadId { get; private set; } = string.Empty;

    /// <summary>
    /// Serialized AgentThread JSON (complete conversation history)
    /// </summary>
    public string SerializedThread { get; private set; } = string.Empty;

    /// <summary>
    /// Thread creation timestamp (UTC)
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Last update timestamp (UTC)
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Total number of messages in conversation
    /// </summary>
    public int MessageCount { get; private set; }

    /// <summary>
    /// Whether thread is active (not archived/deleted)
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Conversation title (auto-generated from first user message)
    /// Max 50 characters
    /// </summary>
    public string? Title { get; private set; }

    /// <summary>
    /// Preview text from last assistant message
    /// Max 100 characters
    /// </summary>
    public string? Preview { get; private set; }

    // Private parameterless constructor for EF Core
    private ConversationThread() { }

    /// <summary>
    /// Factory method to create a new conversation thread
    /// </summary>
    public static ConversationThread Create(
        string threadId,
        string serializedThread,
        string? title = null,
        string? preview = null,
        int messageCount = 0
    )
    {
        if (string.IsNullOrWhiteSpace(threadId))
        {
            throw new ArgumentException("Thread ID cannot be empty", nameof(threadId));
        }

        if (string.IsNullOrWhiteSpace(serializedThread))
        {
            throw new ArgumentException("Serialized thread cannot be empty", nameof(serializedThread));
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;

        return new ConversationThread
        {
            ThreadId = threadId,
            SerializedThread = serializedThread,
            CreatedAt = now,
            UpdatedAt = now,
            MessageCount = messageCount,
            IsActive = true,
            Title = title,
            Preview = preview,
        };
    }

    /// <summary>
    /// Updates the serialized thread content and metadata
    /// </summary>
    public void UpdateThread(
        string serializedThread,
        string? title = null,
        string? preview = null,
        int? messageCount = null
    )
    {
        if (string.IsNullOrWhiteSpace(serializedThread))
        {
            throw new ArgumentException("Serialized thread cannot be empty", nameof(serializedThread));
        }

        SerializedThread = serializedThread;
        UpdatedAt = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(title))
        {
            Title = title;
        }

        if (!string.IsNullOrWhiteSpace(preview))
        {
            Preview = preview;
        }

        if (messageCount.HasValue)
        {
            MessageCount = messageCount.Value;
        }
    }

    /// <summary>
    /// Archives the thread (soft delete)
    /// </summary>
    public void Archive()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Restores archived thread
    /// </summary>
    public void Restore()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
