using TaskAgent.Domain.Constants;
using TaskAgent.Domain.Enums;
using DomainTaskStatus = TaskAgent.Domain.Enums.TaskStatus;

namespace TaskAgent.Domain.Entities;

/// <summary>
/// Represents a task entity in the domain model
/// </summary>
public class TaskItem
{
    public int Id { get; private set; }

    private string _title = string.Empty;
    public string Title
    {
        get => _title;
        private set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(ValidationMessages.TITLE_REQUIRED, nameof(Title));
            }

            if (value.Length > TaskConstants.MAX_TITLE_LENGTH)
            {
                throw new ArgumentException(ValidationMessages.TITLE_TOO_LONG, nameof(Title));
            }

            _title = value;
        }
    }

    public string Description { get; private set; } = string.Empty;
    public TaskPriority Priority { get; private set; }
    public DomainTaskStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private TaskItem() { }

    /// <summary>
    /// Factory method to create a new task
    /// </summary>
    public static TaskItem Create(string title, string description, TaskPriority priority)
    {
        var task = new TaskItem
        {
            Title = title,
            Description = description ?? string.Empty,
            Priority = priority,
            Status = DomainTaskStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        return task;
    }

    /// <summary>
    /// Update task status
    /// </summary>
    public void UpdateStatus(DomainTaskStatus newStatus)
    {
        if (Status == newStatus)
        {
            return;
        }

        if (Status == DomainTaskStatus.Completed && newStatus == DomainTaskStatus.Pending)
        {
            throw new InvalidOperationException(ValidationMessages.CANNOT_REOPEN_COMPLETED_TASK);
        }

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update task priority
    /// </summary>
    public void UpdatePriority(TaskPriority newPriority)
    {
        if (Priority == newPriority)
        {
            return;
        }

        Priority = newPriority;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if task is high priority
    /// </summary>
    public bool IsHighPriority() => Priority == TaskPriority.High;
}
