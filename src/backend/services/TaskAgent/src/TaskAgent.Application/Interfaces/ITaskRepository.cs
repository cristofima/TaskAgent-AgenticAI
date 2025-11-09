using TaskAgent.Domain.Entities;
using DomainTaskPriority = TaskAgent.Domain.Enums.TaskPriority;
using DomainTaskStatus = TaskAgent.Domain.Enums.TaskStatus;

namespace TaskAgent.Application.Interfaces;

/// <summary>
/// Repository interface following Repository Pattern
/// Abstracts data access logic from business logic
/// </summary>
public interface ITaskRepository
{
    // Query operations
    Task<TaskItem?> GetByIdAsync(int id);
    Task<IEnumerable<TaskItem>> GetAllAsync();
    Task<IEnumerable<TaskItem>> SearchAsync(
        DomainTaskStatus? status = null,
        DomainTaskPriority? priority = null
    );

    // Command operations (CRUD)
    Task<TaskItem> AddAsync(TaskItem task);
    Task UpdateAsync(TaskItem task);
    Task DeleteAsync(int id);

    // Unit of Work
    Task<int> SaveChangesAsync();
}
