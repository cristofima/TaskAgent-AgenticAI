using System.ComponentModel;
using System.Text;
using TaskAgent.Application.Constants;
using TaskAgent.Application.Interfaces;
using TaskAgent.Domain.Entities;
using TaskAgent.Domain.Enums;
using DomainTaskStatus = TaskAgent.Domain.Enums.TaskStatus;

namespace TaskAgent.Application.Functions;

/// <summary>
/// Function tools for the Task Management AI Agent
/// Part of Application Layer - orchestrates domain logic and repository operations
/// Follows Single Responsibility Principle - handles AI agent function tool definitions
/// </summary>
public class TaskFunctions
{
    private readonly ITaskRepository _taskRepository;

    public TaskFunctions(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
    }

    [Description("Create a new task with title, description, and priority level.")]
    public async Task<string> CreateTaskAsync(
        [Description("The title of the task")] string title,
        [Description("Detailed description of the task")] string description,
        [Description("Priority level: Low, Medium, or High")] string priority = "Medium"
    )
    {
        try
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return ErrorMessages.TASK_TITLE_EMPTY;
            }

            // Parse priority
            if (!Enum.TryParse<TaskPriority>(priority, true, out TaskPriority taskPriority))
            {
                return string.Format(ErrorMessages.INVALID_PRIORITY_FORMAT, priority);
            }

            // Create task using domain factory method
            var task = TaskItem.Create(title, description ?? string.Empty, taskPriority);

            // Persist to database
            await _taskRepository.AddAsync(task);
            await _taskRepository.SaveChangesAsync();

            return $"""
                {ErrorMessages.TASK_CREATED_SUCCESS}

                ID: {task.Id}
                Title: {task.Title}
                Description: {task.Description}
                Priority: {task.Priority}
                Status: {task.Status}
                Created: {task.CreatedAt:yyyy-MM-dd HH:mm} UTC
                """;
        }
        catch (ArgumentException ex)
        {
            return $"{ErrorMessages.VALIDATION_ERROR_PREFIX}{ex.Message}";
        }
        catch (Exception ex)
        {
            return $"{ErrorMessages.ERROR_CREATING_TASK}{ex.Message}";
        }
    }

    [Description("Get a list of all tasks or filter by status and priority.")]
    public async Task<string> ListTasksAsync(
        [Description("Filter by status: Pending, InProgress, or Completed (optional)")]
            string? status = null,
        [Description("Filter by priority: Low, Medium, or High (optional)")] string? priority = null
    )
    {
        try
        {
            DomainTaskStatus? taskStatus = null;
            TaskPriority? taskPriority = null;

            // Parse filters
            if (!string.IsNullOrEmpty(status))
            {
                if (!Enum.TryParse(status, true, out DomainTaskStatus parsedStatus))
                {
                    return string.Format(ErrorMessages.INVALID_STATUS_FORMAT, status);
                }

                taskStatus = parsedStatus;
            }

            if (!string.IsNullOrEmpty(priority))
            {
                if (!Enum.TryParse(priority, true, out TaskPriority parsedPriority))
                {
                    return string.Format(ErrorMessages.INVALID_PRIORITY_FORMAT, priority);
                }

                taskPriority = parsedPriority;
            }

            // Fetch tasks from repository
            IEnumerable<TaskItem> tasks = await _taskRepository.SearchAsync(
                taskStatus,
                taskPriority
            );
            var taskList = tasks.ToList();

            if (!taskList.Any())
            {
                var filters = new List<string>();
                if (taskStatus.HasValue)
                {
                    filters.Add($"status={taskStatus}");
                }

                if (taskPriority.HasValue)
                {
                    filters.Add($"priority={taskPriority}");
                }

                string filterText = filters.Any() ? $" matching {string.Join(", ", filters)}" : "";
                return string.Format(ErrorMessages.NO_TASKS_FOUND, filterText);
            }

            var result = new StringBuilder();
            result.AppendLine($"📋 Found {taskList.Count} task(s):\n");

            foreach (TaskItem task in taskList)
            {
                string priorityEmoji = task.Priority switch
                {
                    TaskPriority.High => "🔴",
                    TaskPriority.Medium => "🟡",
                    TaskPriority.Low => "🟢",
                    _ => "⚪",
                };

                string statusEmoji = task.Status switch
                {
                    DomainTaskStatus.Completed => "✅",
                    DomainTaskStatus.InProgress => "🔄",
                    DomainTaskStatus.Pending => "⏳",
                    _ => "📌",
                };

                result.AppendLine($"{priorityEmoji} {statusEmoji} Task #{task.Id}: {task.Title}");
                result.AppendLine($"   Description: {task.Description}");
                result.AppendLine($"   Priority: {task.Priority} | Status: {task.Status}");
                result.AppendLine($"   Created: {task.CreatedAt:yyyy-MM-dd HH:mm} UTC");
                result.AppendLine();
            }

            return result.ToString();
        }
        catch (Exception ex)
        {
            return $"{ErrorMessages.ERROR_LISTING_TASKS}{ex.Message}";
        }
    }

    [Description("Get detailed information about a specific task by its ID.")]
    public async Task<string> GetTaskDetailsAsync(
        [Description("The ID of the task to retrieve")] int taskId
    )
    {
        try
        {
            TaskItem? task = await _taskRepository.GetByIdAsync(taskId);

            if (task == null)
            {
                return string.Format(ErrorMessages.TASK_NOT_FOUND, taskId);
            }

            return $"""
                📝 Task Details:

                ID: {task.Id}
                Title: {task.Title}
                Description: {task.Description}
                Priority: {task.Priority}
                Status: {task.Status}
                Created: {task.CreatedAt:yyyy-MM-dd HH:mm} UTC
                Last Updated: {task.UpdatedAt:yyyy-MM-dd HH:mm} UTC
                """;
        }
        catch (Exception ex)
        {
            return $"{ErrorMessages.ERROR_RETRIEVING_TASK}{ex.Message}";
        }
    }

    [Description("Update the status or priority of an existing task.")]
    public async Task<string> UpdateTaskAsync(
        [Description("The ID of the task to update")] int taskId,
        [Description("New status: Pending, InProgress, or Completed (optional)")]
            string? status = null,
        [Description("New priority: Low, Medium, or High (optional)")] string? priority = null
    )
    {
        try
        {
            if (status == null && priority == null)
            {
                return ErrorMessages.UPDATE_REQUIRES_FIELDS;
            }

            // Fetch task
            TaskItem? task = await _taskRepository.GetByIdAsync(taskId);
            if (task == null)
            {
                return string.Format(ErrorMessages.TASK_NOT_FOUND, taskId);
            }

            var updates = new List<string>();

            // Update status if provided
            if (status != null)
            {
                if (!Enum.TryParse(status, true, out DomainTaskStatus newStatus))
                {
                    return string.Format(ErrorMessages.INVALID_STATUS_FORMAT, status);
                }

                task.UpdateStatus(newStatus);
                updates.Add($"status to '{newStatus}'");
            }

            // Update priority if provided
            if (priority != null)
            {
                if (!Enum.TryParse(priority, true, out TaskPriority newPriority))
                {
                    return string.Format(ErrorMessages.INVALID_PRIORITY_FORMAT, priority);
                }

                task.UpdatePriority(newPriority);
                updates.Add($"priority to '{newPriority}'");
            }

            // Persist changes
            await _taskRepository.UpdateAsync(task);
            await _taskRepository.SaveChangesAsync();

            return string.Format(
                ErrorMessages.TASK_UPDATED_SUCCESS,
                taskId,
                string.Join(" and ", updates)
            );
        }
        catch (InvalidOperationException ex)
        {
            return $"{ErrorMessages.BUSINESS_RULE_ERROR_PREFIX}{ex.Message}";
        }
        catch (Exception ex)
        {
            return $"{ErrorMessages.ERROR_UPDATING_TASK}{ex.Message}";
        }
    }

    [Description("Delete a task permanently by its ID.")]
    public async Task<string> DeleteTaskAsync(
        [Description("The ID of the task to delete")] int taskId
    )
    {
        try
        {
            TaskItem? task = await _taskRepository.GetByIdAsync(taskId);
            if (task == null)
            {
                return string.Format(ErrorMessages.TASK_NOT_FOUND, taskId);
            }

            await _taskRepository.DeleteAsync(taskId);
            await _taskRepository.SaveChangesAsync();

            return string.Format(ErrorMessages.TASK_DELETED_SUCCESS, taskId, task.Title);
        }
        catch (Exception ex)
        {
            return $"{ErrorMessages.ERROR_DELETING_TASK}{ex.Message}";
        }
    }

    [Description("Get a summary of all tasks grouped by status.")]
    public async Task<string> GetTaskSummaryAsync()
    {
        try
        {
            var allTasks = (await _taskRepository.GetAllAsync()).ToList();

            if (allTasks.Count == 0)
            {
                return ErrorMessages.NO_TASKS_IN_SYSTEM;
            }

            int pending = allTasks.Count(t => t.Status == DomainTaskStatus.Pending);
            int inProgress = allTasks.Count(t => t.Status == DomainTaskStatus.InProgress);
            int completed = allTasks.Count(t => t.Status == DomainTaskStatus.Completed);

            int highPriority = allTasks.Count(t =>
                t.Status != DomainTaskStatus.Completed && t.IsHighPriority()
            );

            int completionRate = completed * 100 / allTasks.Count;

            return $"""
                📊 Task Summary:

                Total Tasks: {allTasks.Count}

                By Status:
                ⏳ Pending: {pending}
                🔄 In Progress: {inProgress}
                ✅ Completed: {completed}

                🔴 High Priority (not completed): {highPriority}

                Completion Rate: {completionRate}%
                """;
        }
        catch (Exception ex)
        {
            return $"{ErrorMessages.ERROR_GENERATING_SUMMARY}{ex.Message}";
        }
    }
}
