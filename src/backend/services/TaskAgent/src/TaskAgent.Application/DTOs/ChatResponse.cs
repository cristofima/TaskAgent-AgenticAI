namespace TaskAgent.Application.DTOs;

/// <summary>
/// Data Transfer Object for chat responses
/// </summary>
public record ChatResponse
{
    /// <summary>
    /// The response message from the AI agent
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Thread ID for conversation continuity
    /// </summary>
    public string ThreadId { get; init; } = string.Empty;

    /// <summary>
    /// Unique identifier for this response message
    /// </summary>
    public string MessageId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Timestamp when the response was created
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Metadata about the response (function calls, citations, etc.)
    /// </summary>
    public MessageMetadata? Metadata { get; init; }

    /// <summary>
    /// Contextual suggestions for next user actions
    /// </summary>
    public IReadOnlyList<string>? Suggestions { get; init; }
}
