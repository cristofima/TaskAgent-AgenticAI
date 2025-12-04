namespace TaskAgent.Application.DTOs.Responses;

/// <summary>
/// Response for listing conversation threads
/// </summary>
public class ListThreadsResponseDto
{
    public required List<ConversationThreadDto> Threads { get; set; }
    public required int TotalCount { get; set; }
    public required int Page { get; set; }
    public required int PageSize { get; set; }
    public required int TotalPages { get; set; }
}

/// <summary>
/// Conversation thread summary with metadata
/// </summary>
public class ConversationThreadDto
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public required string Preview { get; set; }
    public required int MessageCount { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public required DateTimeOffset UpdatedAt { get; set; }
    public required bool IsActive { get; set; }
    public string? SerializedState { get; set; } // AG-UI protocol: serialized thread state for resuming
}

/// <summary>
/// Response for getting conversation history
/// </summary>
public class GetConversationResponseDto
{
    public required string ThreadId { get; set; }
    public string? SerializedState { get; set; } // AG-UI protocol: serialized thread state for resuming
    public required List<ConversationMessageDto> Messages { get; set; }
    public required int TotalCount { get; set; }
    public required int Page { get; set; }
    public required int PageSize { get; set; }
}

/// <summary>
/// Individual conversation message
/// </summary>
public class ConversationMessageDto
{
    public required string MessageId { get; set; }
    public required string Role { get; set; }
    public required string Content { get; set; } // JSON string (serialized List<AIContent>)
    public required DateTimeOffset Timestamp { get; set; }
}
