namespace TaskAgent.Application.DTOs;

/// <summary>
/// Request to list conversation threads
/// </summary>
public record ListThreadsRequest
{
    /// <summary>
    /// Page number for pagination (1-based)
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Number of threads per page
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Sort by field (CreatedAt or UpdatedAt)
    /// </summary>
    public string SortBy { get; init; } = "UpdatedAt";

    /// <summary>
    /// Sort order (asc or desc)
    /// </summary>
    public string SortOrder { get; init; } = "desc";

    /// <summary>
    /// Filter by active status (null = all)
    /// </summary>
    public bool? IsActive { get; init; }
}

/// <summary>
/// Response with list of conversation threads
/// </summary>
public record ListThreadsResponse
{
    /// <summary>
    /// List of conversation threads
    /// </summary>
    public IReadOnlyList<ConversationThreadDTO> Threads { get; init; } = [];

    /// <summary>
    /// Total number of threads
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Number of threads per page
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Whether there are more threads to load
    /// </summary>
    public bool HasMore { get; init; }
}
