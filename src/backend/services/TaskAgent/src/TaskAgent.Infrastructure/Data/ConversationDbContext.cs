using Microsoft.EntityFrameworkCore;
using TaskAgent.Domain.Constants;
using TaskAgent.Domain.Entities;

namespace TaskAgent.Infrastructure.Data;

/// <summary>
/// Database context for Conversation Threads stored in PostgreSQL
/// Stores complete serialized AgentThread as JSON blob
/// </summary>
public class ConversationDbContext : DbContext
{
    public ConversationDbContext(DbContextOptions<ConversationDbContext> options)
        : base(options) { }

    public DbSet<ConversationThread> ConversationThreads => Set<ConversationThread>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ConversationThread>(entity =>
        {
            // Table name
            entity.ToTable("ConversationThreads");

            // Primary key
            entity.HasKey(e => e.ThreadId);

            // Properties configuration
            entity
                .Property(e => e.ThreadId)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("varchar(100)");

            entity
                .Property(e => e.SerializedThread)
                .IsRequired()
                .HasColumnType("json");

            entity.Property(e => e.CreatedAt).IsRequired().HasColumnType("timestamptz");

            entity.Property(e => e.UpdatedAt).IsRequired().HasColumnType("timestamptz");

            entity.Property(e => e.MessageCount).IsRequired().HasDefaultValue(0);

            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

            entity
                .Property(e => e.Title)
                .HasMaxLength(ConversationThreadConstants.MAX_TITLE_LENGTH);

            entity
                .Property(e => e.Preview)
                .HasMaxLength(ConversationThreadConstants.MAX_PREVIEW_LENGTH);

            // Indexes for better query performance
            entity
                .HasIndex(e => e.UpdatedAt)
                .HasDatabaseName("IX_ConversationThreads_UpdatedAt")
                .IsDescending();

            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_ConversationThreads_IsActive");

            entity
                .HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_ConversationThreads_CreatedAt")
                .IsDescending();
        });
    }
}
