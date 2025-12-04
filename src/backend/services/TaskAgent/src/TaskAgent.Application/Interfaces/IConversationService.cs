using TaskAgent.Application.DTOs.Responses;

namespace TaskAgent.Application.Interfaces;

/// <summary>
/// Service for managing conversation threads and messages.
/// </summary>
public interface IConversationService
{
    /// <summary>
    /// Lists conversation threads with pagination and sorting.
    /// </summary>
    Task<ListThreadsResponseDto> ListThreadsAsync(
        int page,
        int pageSize,
        string sortBy,
        string sortOrder,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets conversation history for a specific thread.
    /// </summary>
    Task<GetConversationResponseDto?> GetConversationHistoryAsync(
        string threadId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Deletes a conversation thread.
    /// </summary>
    Task<bool> DeleteThreadAsync(string threadId, CancellationToken cancellationToken = default);
}
