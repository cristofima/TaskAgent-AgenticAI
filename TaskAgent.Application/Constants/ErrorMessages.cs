namespace TaskAgent.Application.Constants;

/// <summary>
/// Application-level error messages for function tools and services
/// </summary>
public static class ErrorMessages
{
    // General errors
    public const string TASK_TITLE_EMPTY = "❌ Error: Task title cannot be empty.";
    public const string INVALID_PRIORITY_FORMAT = "❌ Error: Invalid priority '{0}'. Must be: Low, Medium, or High.";
    public const string INVALID_STATUS_FORMAT = "❌ Invalid status '{0}'. Must be: Pending, InProgress, or Completed.";
    public const string TASK_NOT_FOUND = "❌ Task #{0} not found.";
    public const string UPDATE_REQUIRES_FIELDS = "❌ Error: You must specify either status or priority to update.";
    public const string NO_TASKS_FOUND = "📋 No tasks found{0}.";
    public const string NO_TASKS_IN_SYSTEM = "📊 No tasks in the system yet.";
    
    // Success messages
    public const string TASK_CREATED_SUCCESS = "✅ Task created successfully!";
    public const string TASK_UPDATED_SUCCESS = "✅ Task #{0} updated successfully! Changed {1}.";
    public const string TASK_DELETED_SUCCESS = "✅ Task #{0} '{1}' has been deleted successfully.";
    
    // Error prefixes
    public const string VALIDATION_ERROR_PREFIX = "❌ Validation error: ";
    public const string BUSINESS_RULE_ERROR_PREFIX = "❌ Business rule violation: ";
    public const string ERROR_CREATING_TASK = "❌ Error creating task: ";
    public const string ERROR_LISTING_TASKS = "❌ Error listing tasks: ";
    public const string ERROR_RETRIEVING_TASK = "❌ Error retrieving task: ";
    public const string ERROR_UPDATING_TASK = "❌ Error updating task: ";
    public const string ERROR_DELETING_TASK = "❌ Error deleting task: ";
    public const string ERROR_GENERATING_SUMMARY = "❌ Error generating summary: ";
}
