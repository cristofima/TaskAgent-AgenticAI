using System.ComponentModel;
using System.Text;
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
    public async Task<string> CreateTask(
        [Description("The title of the task")] string title,
        [Description("Detailed description of the task")] string description,
        [Description("Priority level: Low, Medium, or High")] string priority = "Medium"
    )
    {
        try
        {
            if (string.IsNullOrWhiteSpace(title))
                return "❌ Error: Task title cannot be empty.";

            // Parse priority
            if (!Enum.TryParse<TaskPriority>(priority, true, out var taskPriority))
            {
                return $"❌ Error: Invalid priority '{priority}'. Must be: Low, Medium, or High.";
            }

            // Create task using domain factory method
            var task = TaskItem.Create(title, description ?? string.Empty, taskPriority);

            // Persist to database
            await _taskRepository.AddAsync(task);
            await _taskRepository.SaveChangesAsync();

            return $"""
                ✅ Task created successfully!

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
            return $"❌ Validation error: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"❌ Error creating task: {ex.Message}";
        }
    }

    [Description("Get a list of all tasks or filter by status and priority.")]
    public async Task<string> ListTasks(
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
                if (!Enum.TryParse<DomainTaskStatus>(status, true, out var parsedStatus))
                    return $"❌ Invalid status '{status}'. Must be: Pending, InProgress, or Completed.";
                taskStatus = parsedStatus;
            }

            if (!string.IsNullOrEmpty(priority))
            {
                if (!Enum.TryParse<TaskPriority>(priority, true, out var parsedPriority))
                    return $"❌ Invalid priority '{priority}'. Must be: Low, Medium, or High.";
                taskPriority = parsedPriority;
            }

            // Fetch tasks from repository
            var tasks = await _taskRepository.SearchAsync(taskStatus, taskPriority);
            var taskList = tasks.ToList();

            if (!taskList.Any())
            {
                var filters = new List<string>();
                if (taskStatus.HasValue)
                    filters.Add($"status={taskStatus}");
                if (taskPriority.HasValue)
                    filters.Add($"priority={taskPriority}");

                var filterText = filters.Any() ? $" matching {string.Join(", ", filters)}" : "";
                return $"📋 No tasks found{filterText}.";
            }

            var result = new StringBuilder();
            result.AppendLine($"📋 Found {taskList.Count} task(s):\n");

            foreach (var task in taskList)
            {
                var priorityEmoji = task.Priority switch
                {
                    TaskPriority.High => "🔴",
                    TaskPriority.Medium => "🟡",
                    TaskPriority.Low => "🟢",
                    _ => "⚪",
                };

                var statusEmoji = task.Status switch
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
            return $"❌ Error listing tasks: {ex.Message}";
        }
    }

    [Description("Get detailed information about a specific task by its ID.")]
    public async Task<string> GetTaskDetails(
        [Description("The ID of the task to retrieve")] int taskId
    )
    {
        try
        {
            var task = await _taskRepository.GetByIdAsync(taskId);

            if (task == null)
                return $"❌ Task #{taskId} not found.";

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
            return $"❌ Error retrieving task: {ex.Message}";
        }
    }

    [Description("Update the status or priority of an existing task.")]
    public async Task<string> UpdateTask(
        [Description("The ID of the task to update")] int taskId,
        [Description("New status: Pending, InProgress, or Completed (optional)")]
            string? status = null,
        [Description("New priority: Low, Medium, or High (optional)")] string? priority = null
    )
    {
        try
        {
            if (status == null && priority == null)
                return "❌ Error: You must specify either status or priority to update.";

            // Fetch task
            var task = await _taskRepository.GetByIdAsync(taskId);
            if (task == null)
                return $"❌ Task #{taskId} not found.";

            var updates = new List<string>();

            // Update status if provided
            if (status != null)
            {
                if (!Enum.TryParse<DomainTaskStatus>(status, true, out var newStatus))
                    return $"❌ Invalid status '{status}'. Must be: Pending, InProgress, or Completed.";

                task.UpdateStatus(newStatus);
                updates.Add($"status to '{newStatus}'");
            }

            // Update priority if provided
            if (priority != null)
            {
                if (!Enum.TryParse<TaskPriority>(priority, true, out var newPriority))
                    return $"❌ Invalid priority '{priority}'. Must be: Low, Medium, or High.";

                task.UpdatePriority(newPriority);
                updates.Add($"priority to '{newPriority}'");
            }

            // Persist changes
            await _taskRepository.UpdateAsync(task);
            await _taskRepository.SaveChangesAsync();

            return $"✅ Task #{taskId} updated successfully! Changed {string.Join(" and ", updates)}.";
        }
        catch (InvalidOperationException ex)
        {
            return $"❌ Business rule violation: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"❌ Error updating task: {ex.Message}";
        }
    }

    [Description("Delete a task permanently by its ID.")]
    public async Task<string> DeleteTask([Description("The ID of the task to delete")] int taskId)
    {
        try
        {
            var task = await _taskRepository.GetByIdAsync(taskId);
            if (task == null)
                return $"❌ Task #{taskId} not found.";

            await _taskRepository.DeleteAsync(taskId);
            await _taskRepository.SaveChangesAsync();

            return $"✅ Task #{taskId} '{task.Title}' has been deleted successfully.";
        }
        catch (Exception ex)
        {
            return $"❌ Error deleting task: {ex.Message}";
        }
    }

    [Description("Get a summary of all tasks grouped by status.")]
    public async Task<string> GetTaskSummary()
    {
        try
        {
            var allTasks = (await _taskRepository.GetAllAsync()).ToList();

            if (!allTasks.Any())
                return "📊 No tasks in the system yet.";

            var pending = allTasks.Count(t => t.Status == DomainTaskStatus.Pending);
            var inProgress = allTasks.Count(t => t.Status == DomainTaskStatus.InProgress);
            var completed = allTasks.Count(t => t.Status == DomainTaskStatus.Completed);

            var highPriority = allTasks.Count(t =>
                t.Status != DomainTaskStatus.Completed && t.IsHighPriority()
            );

            var completionRate = allTasks.Count > 0 ? (completed * 100 / allTasks.Count) : 0;

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
            return $"❌ Error generating summary: {ex.Message}";
        }
    }
}
