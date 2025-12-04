using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskAgent.Application.Constants;
using TaskAgent.Application.Interfaces;
using TaskAgent.Application.Telemetry;
using TaskAgent.Domain.Entities;
using TaskAgent.Domain.Enums;
using DomainTaskStatus = TaskAgent.Domain.Enums.TaskStatus;

namespace TaskAgent.Application.Functions;

/// <summary>
/// Provides function tools for the Task Management AI Agent with automatic AI function calling support.
/// </summary>
/// <remarks>
/// Uses <see cref="IServiceProvider"/> to resolve scoped dependencies per function call, ensuring fresh DbContext instances.
/// All methods return user-friendly strings and never throw exceptions to the AI.
/// </remarks>
public class TaskFunctions
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AgentMetrics _metrics;
    private readonly ILogger<TaskFunctions> _logger;

    public TaskFunctions(
        IServiceProvider serviceProvider,
        AgentMetrics metrics,
        ILogger<TaskFunctions> logger
    )
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Description("Create a new task with title, description, and priority level.")]
    public async Task<string> CreateTaskAsync(
        [Description("The title of the task")] string title,
        [Description("Detailed description of the task")] string description,
        [Description("Priority level: Low, Medium, or High")] string priority = "Medium"
    )
    {
        using Activity? activity = AgentActivitySource.StartFunctionActivity(
            "CreateTask",
            new Dictionary<string, object?> { ["task.title"] = title, ["task.priority"] = priority }
        );

        try
        {
            _metrics.RecordFunctionCall("CreateTask");
            _logger.LogInformation(
                "CreateTask function called with title: {Title}, priority: {Priority}",
                title,
                priority
            );

            if (string.IsNullOrWhiteSpace(title))
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Title is empty");
                return ErrorMessages.TASK_TITLE_EMPTY;
            }

            // Parse priority
            if (!Enum.TryParse(priority, true, out TaskPriority taskPriority))
            {
                activity?.SetStatus(ActivityStatusCode.Error, $"Invalid priority: {priority}");
                return string.Format(ErrorMessages.INVALID_PRIORITY_FORMAT, priority);
            }

            // Create task using domain factory method
            var task = TaskItem.Create(title, description ?? string.Empty, taskPriority);

            // Create scope to resolve scoped ITaskRepository
            using IServiceScope scope = _serviceProvider.CreateScope();
            ITaskRepository taskRepository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();

            // Persist to database
            await taskRepository.AddAsync(task);
            await taskRepository.SaveChangesAsync();

            activity?.SetTag("task.id", task.Id);
            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger.LogInformation("Task created successfully with ID: {TaskId}", task.Id);

            return $"""
                {SuccessMessages.TASK_CREATED_SUCCESS}

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
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogWarning(ex, "Validation error in CreateTask: {Message}", ex.Message);
            return $"{ErrorMessages.VALIDATION_ERROR_PREFIX}{ex.Message}";
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error creating task");
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
        using Activity? activity = AgentActivitySource.StartFunctionActivity(
            "ListTasks",
            new Dictionary<string, object?>
            {
                ["filter.status"] = status,
                ["filter.priority"] = priority,
            }
        );

        try
        {
            _metrics.RecordFunctionCall("ListTasks");
            _logger.LogInformation(
                "ListTasks function called with filters - status: {Status}, priority: {Priority}",
                status,
                priority
            );

            DomainTaskStatus? taskStatus = null;
            TaskPriority? taskPriority = null;

            // Parse filters
            if (!string.IsNullOrEmpty(status))
            {
                if (!Enum.TryParse(status, true, out DomainTaskStatus parsedStatus))
                {
                    activity?.SetStatus(ActivityStatusCode.Error, $"Invalid status: {status}");
                    return string.Format(ErrorMessages.INVALID_STATUS_FORMAT, status);
                }

                taskStatus = parsedStatus;
            }

            if (!string.IsNullOrEmpty(priority))
            {
                if (!Enum.TryParse(priority, true, out TaskPriority parsedPriority))
                {
                    activity?.SetStatus(ActivityStatusCode.Error, $"Invalid priority: {priority}");
                    return string.Format(ErrorMessages.INVALID_PRIORITY_FORMAT, priority);
                }

                taskPriority = parsedPriority;
            }

            // Create scope to resolve scoped ITaskRepository
            using IServiceScope scope = _serviceProvider.CreateScope();
            ITaskRepository taskRepository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();

            // Fetch tasks from repository
            IEnumerable<TaskItem> tasks = await taskRepository.SearchAsync(
                taskStatus,
                taskPriority
            );
            var taskList = tasks.ToList();

            activity?.SetTag("result.count", taskList.Count);
            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger.LogInformation(
                "ListTasks completed successfully - found {Count} tasks",
                taskList.Count
            );

            if (taskList.Count == 0)
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
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error listing tasks");
            return $"{ErrorMessages.ERROR_LISTING_TASKS}{ex.Message}";
        }
    }

    [Description("Get detailed information about a specific task by its ID.")]
    public async Task<string> GetTaskDetailsAsync(
        [Description("The ID of the task to retrieve")] int taskId
    )
    {
        using Activity? activity = AgentActivitySource.StartFunctionActivity(
            "GetTaskDetails",
            new Dictionary<string, object?> { ["task.id"] = taskId }
        );

        try
        {
            _metrics.RecordFunctionCall("GetTaskDetails");
            _logger.LogInformation("GetTaskDetails function called for task ID: {TaskId}", taskId);

            // Create scope to resolve scoped ITaskRepository
            using IServiceScope scope = _serviceProvider.CreateScope();
            ITaskRepository taskRepository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();

            TaskItem? task = await taskRepository.GetByIdAsync(taskId);

            if (task == null)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Task not found");
                _logger.LogWarning("Task not found with ID: {TaskId}", taskId);
                return string.Format(ErrorMessages.TASK_NOT_FOUND, taskId);
            }

            activity?.SetTag("task.title", task.Title);
            activity?.SetTag("task.status", task.Status.ToString());
            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger.LogInformation("Task details retrieved successfully for ID: {TaskId}", taskId);

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
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error retrieving task details for ID: {TaskId}", taskId);
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
        using Activity? activity = AgentActivitySource.StartFunctionActivity(
            "UpdateTask",
            new Dictionary<string, object?>
            {
                ["task.id"] = taskId,
                ["update.status"] = status,
                ["update.priority"] = priority,
            }
        );

        try
        {
            _metrics.RecordFunctionCall("UpdateTask");
            _logger.LogInformation(
                "UpdateTask function called for task ID: {TaskId} - status: {Status}, priority: {Priority}",
                taskId,
                status,
                priority
            );

            if (status == null && priority == null)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "No fields to update");
                return ErrorMessages.UPDATE_REQUIRES_FIELDS;
            }

            // Create scope to resolve scoped ITaskRepository
            using IServiceScope scope = _serviceProvider.CreateScope();
            ITaskRepository taskRepository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();

            // Fetch task
            TaskItem? task = await taskRepository.GetByIdAsync(taskId);
            if (task == null)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Task not found");
                _logger.LogWarning("Task not found with ID: {TaskId}", taskId);
                return string.Format(ErrorMessages.TASK_NOT_FOUND, taskId);
            }

            var updates = new List<string>();

            // Update status if provided
            if (status != null)
            {
                if (!Enum.TryParse(status, true, out DomainTaskStatus newStatus))
                {
                    activity?.SetStatus(ActivityStatusCode.Error, $"Invalid status: {status}");
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
                    activity?.SetStatus(ActivityStatusCode.Error, $"Invalid priority: {priority}");
                    return string.Format(ErrorMessages.INVALID_PRIORITY_FORMAT, priority);
                }

                task.UpdatePriority(newPriority);
                updates.Add($"priority to '{newPriority}'");
            }

            // Persist changes
            await taskRepository.UpdateAsync(task);
            await taskRepository.SaveChangesAsync();

            activity?.SetTag("updates.applied", string.Join(", ", updates));
            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger.LogInformation(
                "Task {TaskId} updated successfully - {Updates}",
                taskId,
                string.Join(", ", updates)
            );

            return string.Format(
                SuccessMessages.TASK_UPDATED_SUCCESS,
                taskId,
                string.Join(" and ", updates)
            );
        }
        catch (InvalidOperationException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogWarning(ex, "Business rule violation in UpdateTask: {Message}", ex.Message);
            return $"{ErrorMessages.BUSINESS_RULE_ERROR_PREFIX}{ex.Message}";
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error updating task ID: {TaskId}", taskId);
            return $"{ErrorMessages.ERROR_UPDATING_TASK}{ex.Message}";
        }
    }

    [Description("Delete a task permanently by its ID.")]
    public async Task<string> DeleteTaskAsync(
        [Description("The ID of the task to delete")] int taskId
    )
    {
        using Activity? activity = AgentActivitySource.StartFunctionActivity(
            "DeleteTask",
            new Dictionary<string, object?> { ["task.id"] = taskId }
        );

        try
        {
            _metrics.RecordFunctionCall("DeleteTask");
            _logger.LogInformation("DeleteTask function called for task ID: {TaskId}", taskId);

            // Create scope to resolve scoped ITaskRepository
            using IServiceScope scope = _serviceProvider.CreateScope();
            ITaskRepository taskRepository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();

            TaskItem? task = await taskRepository.GetByIdAsync(taskId);
            if (task == null)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Task not found");
                _logger.LogWarning("Task not found with ID: {TaskId}", taskId);
                return string.Format(ErrorMessages.TASK_NOT_FOUND, taskId);
            }

            await taskRepository.DeleteAsync(taskId);
            await taskRepository.SaveChangesAsync();

            activity?.SetTag("task.title", task.Title);
            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger.LogInformation(
                "Task {TaskId} deleted successfully - title: {Title}",
                taskId,
                task.Title
            );

            return string.Format(SuccessMessages.TASK_DELETED_SUCCESS, taskId, task.Title);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error deleting task ID: {TaskId}", taskId);
            return $"{ErrorMessages.ERROR_DELETING_TASK}{ex.Message}";
        }
    }

    [Description("Get a summary of all tasks grouped by status.")]
    public async Task<string> GetTaskSummaryAsync()
    {
        using Activity? activity = AgentActivitySource.StartFunctionActivity(
            "GetTaskSummary",
            new Dictionary<string, object?>()
        );

        try
        {
            _metrics.RecordFunctionCall("GetTaskSummary");
            _logger.LogInformation("GetTaskSummary function called");

            // Create scope to resolve scoped ITaskRepository
            using IServiceScope scope = _serviceProvider.CreateScope();
            ITaskRepository taskRepository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();

            IEnumerable<TaskItem> allTasksEnumerable = await taskRepository.GetAllAsync();
            var allTasks = allTasksEnumerable.ToList(); // Convert to List for Count property

            if (allTasks.Count == 0)
            {
                activity?.SetTag("result.count", 0);
                activity?.SetStatus(ActivityStatusCode.Ok);
                _logger.LogInformation("GetTaskSummary completed - no tasks in system");
                return ErrorMessages.NO_TASKS_IN_SYSTEM;
            }

            int pending = allTasks.Count(t => t.Status == DomainTaskStatus.Pending);
            int inProgress = allTasks.Count(t => t.Status == DomainTaskStatus.InProgress);
            int completed = allTasks.Count(t => t.Status == DomainTaskStatus.Completed);

            int highPriority = allTasks.Count(t =>
                t.Status != DomainTaskStatus.Completed && t.IsHighPriority()
            );

            int completionRate = completed * 100 / allTasks.Count;

            activity?.SetTag("result.total", allTasks.Count);
            activity?.SetTag("result.pending", pending);
            activity?.SetTag("result.inProgress", inProgress);
            activity?.SetTag("result.completed", completed);
            activity?.SetTag("result.completionRate", completionRate);
            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger.LogInformation(
                "GetTaskSummary completed successfully - total: {Total}, pending: {Pending}, inProgress: {InProgress}, completed: {Completed}",
                allTasks.Count,
                pending,
                inProgress,
                completed
            );

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
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error generating task summary");
            return $"{ErrorMessages.ERROR_GENERATING_SUMMARY}{ex.Message}";
        }
    }
}
