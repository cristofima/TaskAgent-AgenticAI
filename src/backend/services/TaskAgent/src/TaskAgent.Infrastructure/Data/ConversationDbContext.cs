using Microsoft.EntityFrameworkCore;
using TaskAgent.Domain.Entities;
using TaskAgent.Infrastructure.Data.Configurations;

namespace TaskAgent.Infrastructure.Data;

/// <summary>
/// PostgreSQL database context for conversation messages and thread metadata.
/// </summary>
/// <remarks>
/// Follows Microsoft Agent Framework <c>ChatMessageStore</c> pattern for message persistence.
/// </remarks>
public class ConversationDbContext : DbContext
{
    public ConversationDbContext(DbContextOptions<ConversationDbContext> options)
        : base(options) { }

    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();
    public DbSet<ConversationThreadMetadata> ConversationThreads => Set<ConversationThreadMetadata>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ConversationMessage>(entity =>
        {
            // Table name
            entity.ToTable("ConversationMessages");

            // Primary key
            entity.HasKey(e => e.MessageId);

            // Properties configuration
            entity
                .Property(e => e.MessageId)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("varchar(50)");

            entity
                .Property(e => e.ThreadId)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("varchar(50)");

            entity
                .Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnType("varchar(20)");

            entity.Property(e => e.Content).IsRequired().HasColumnType("json");

            entity.Property(e => e.Timestamp).IsRequired().HasColumnType("timestamptz");

            // Indexes for better query performance
            entity
                .HasIndex(e => new { e.ThreadId, e.Timestamp })
                .HasDatabaseName("IX_ConversationMessages_ThreadId_Timestamp");

            entity.HasIndex(e => e.ThreadId).HasDatabaseName("IX_ConversationMessages_ThreadId");
        });

        // Apply ConversationThreadMetadata configuration
        modelBuilder.ApplyConfiguration(new ConversationThreadMetadataConfiguration());
    }
}
