using Microsoft.EntityFrameworkCore;
using TaskAgent.Domain.Constants;
using TaskAgent.Domain.Entities;

namespace TaskAgent.Infrastructure.Data;

/// <summary>
/// Database context for Task entities stored in SQL Server
/// </summary>
public class TaskDbContext : DbContext
{
    public TaskDbContext(DbContextOptions<TaskDbContext> options)
        : base(options) { }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure TaskItem entity
        modelBuilder.Entity<TaskItem>(entity =>
        {
            // Table name
            entity.ToTable("Tasks");

            // Primary key
            entity.HasKey(e => e.Id);

            // Properties configuration
            entity
                .Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(TaskConstants.MAX_TITLE_LENGTH);

            entity.Property(e => e.Description).HasMaxLength(TaskConstants.MAX_DESCRIPTION_LENGTH);

            entity.Property(e => e.Priority).IsRequired().HasConversion<int>(); // Store enum as int

            entity.Property(e => e.Status).IsRequired().HasConversion<int>(); // Store enum as int

            entity.Property(e => e.CreatedAt).IsRequired();

            entity.Property(e => e.UpdatedAt).IsRequired();

            // Indexes for better query performance
            entity.HasIndex(e => e.Status).HasDatabaseName("IX_Tasks_Status");

            entity.HasIndex(e => e.Priority).HasDatabaseName("IX_Tasks_Priority");

            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_Tasks_CreatedAt");
        });
    }
}
