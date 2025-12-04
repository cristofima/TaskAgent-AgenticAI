namespace TaskAgent.Application.Validators;

/// <summary>
/// Validator for pagination parameters.
/// </summary>
public static class PaginationValidator
{
    private const int MIN_PAGE = 1;
    private const int MIN_PAGE_SIZE = 1;
    private const int MAX_PAGE_SIZE = 100;

    /// <summary>
    /// Validates pagination parameters.
    /// </summary>
    /// <returns>Error message if invalid, null if valid.</returns>
    public static string? ValidatePagination(int page, int pageSize)
    {
        if (page < MIN_PAGE)
        {
            return $"Page must be at least {MIN_PAGE}";
        }

        if (pageSize < MIN_PAGE_SIZE)
        {
            return $"Page size must be at least {MIN_PAGE_SIZE}";
        }

        if (pageSize > MAX_PAGE_SIZE)
        {
            return $"Page size cannot exceed {MAX_PAGE_SIZE}";
        }

        return null;
    }

    /// <summary>
    /// Validates thread ID.
    /// </summary>
    /// <returns>Error message if invalid, null if valid.</returns>
    public static string? ValidateThreadId(string? threadId)
    {
        if (string.IsNullOrWhiteSpace(threadId))
        {
            return "ThreadId cannot be empty";
        }

        return null;
    }
}
