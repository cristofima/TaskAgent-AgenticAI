namespace TaskAgent.Application.DTOs;

/// <summary>
/// Request to get conversation history for a thread
/// </summary>
public record GetConversationRequest
{
    /// <summary>
    /// Thread ID to retrieve messages from
    /// </summary>
    public string ThreadId { get; init; } = string.Empty;

    /// <summary>
    /// Page number for pagination (1-based)
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Number of messages per page
    /// </summary>
    public int PageSize { get; init; } = 50;
}

/// <summary>
/// Response containing conversation history
/// </summary>
public record GetConversationResponse
{
    /// <summary>
    /// Thread ID
    /// </summary>
    public string ThreadId { get; init; } = string.Empty;

    /// <summary>
    /// List of messages in the conversation
    /// </summary>
    public IReadOnlyList<ChatMessage> Messages { get; init; } = [];

    /// <summary>
    /// Total number of messages in the thread
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Number of messages per page
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Whether there are more messages to load
    /// </summary>
    public bool HasMore { get; init; }
}
