using Microsoft.EntityFrameworkCore;
using TaskAgent.Application.Interfaces;
using TaskAgent.Domain.Entities;
using TaskAgent.Domain.Enums;
using TaskAgent.Infrastructure.Data;
using DomainTaskStatus = TaskAgent.Domain.Enums.TaskStatus;

namespace TaskAgent.Infrastructure.Repositories;

/// <summary>
/// Implementation of ITaskRepository using Entity Framework Core
/// Follows Repository Pattern to encapsulate data access logic
/// All CRUD operations are implemented for the AI Agent
/// All queries are scoped to the current user for multi-user support
/// </summary>
public class TaskRepository : ITaskRepository
{
    private readonly TaskDbContext _context;

    public TaskRepository(TaskDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<TaskItem?> GetByIdAsync(int id, string userId) =>
        await _context.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

    public async Task<IEnumerable<TaskItem>> GetAllByUserAsync(string userId)
    {
        return await _context
            .Tasks.AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskItem>> SearchAsync(
        string userId,
        DomainTaskStatus? status = null,
        TaskPriority? priority = null
    )
    {
        IQueryable<TaskItem> query = _context.Tasks
            .AsNoTracking()
            .Where(t => t.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(t => t.Priority == priority.Value);
        }

        return await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
    }

    public async Task<TaskItem> AddAsync(TaskItem task)
    {
        if (task == null)
        {
            throw new ArgumentNullException(nameof(task));
        }

        await _context.Tasks.AddAsync(task);
        return task;
    }

    public Task UpdateAsync(TaskItem task)
    {
        if (task == null)
        {
            throw new ArgumentNullException(nameof(task));
        }

        _context.Tasks.Update(task);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(int id, string userId)
    {
        TaskItem? task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (task != null)
        {
            _context.Tasks.Remove(task);
        }
    }

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
}
