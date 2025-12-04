using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskAgent.Domain.Entities;

namespace TaskAgent.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for ConversationThreadMetadata entity
/// </summary>
public class ConversationThreadMetadataConfiguration : IEntityTypeConfiguration<ConversationThreadMetadata>
{
    public void Configure(EntityTypeBuilder<ConversationThreadMetadata> builder)
    {
        // Table name
        builder.ToTable("ConversationThreads");

        // Primary key
        builder.HasKey(t => t.ThreadId);

        // Properties
        builder.Property(t => t.ThreadId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.Title)
            .HasMaxLength(200);

        builder.Property(t => t.Preview)
            .HasMaxLength(500);

        builder.Property(t => t.MessageCount)
            .IsRequired();

        builder.Property(t => t.SerializedState)
            .HasColumnType("text"); // PostgreSQL text type for large JSON

        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.Property(t => t.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.Property(t => t.IsActive)
            .IsRequired();

        // Indexes
        builder.HasIndex(t => t.IsActive)
            .HasDatabaseName("IX_ConversationThreads_IsActive");

        builder.HasIndex(t => t.UpdatedAt)
            .HasDatabaseName("IX_ConversationThreads_UpdatedAt");

        builder.HasIndex(t => new { t.IsActive, t.UpdatedAt })
            .HasDatabaseName("IX_ConversationThreads_IsActive_UpdatedAt");
    }
}
