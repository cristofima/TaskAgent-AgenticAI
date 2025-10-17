namespace TaskAgent.Domain.Constants;

/// <summary>
/// Validation error messages for domain entities
/// </summary>
public static class ValidationMessages
{
    public const string TITLE_REQUIRED = "Title cannot be empty";
    public const string TITLE_TOO_LONG = "Title cannot exceed 200 characters";
    public const string CANNOT_REOPEN_COMPLETED_TASK =
        "Cannot change completed task back to pending";
}
