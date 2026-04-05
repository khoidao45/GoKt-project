using Gokt.Domain.Entities;
using Gokt.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gokt.Infrastructure.Persistence.Configurations;

public class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        builder.ToTable("OutboxEvents");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Type)
            .IsRequired()
            .HasMaxLength(200);

        // Payload stored as TEXT (PostgreSQL can index JSONB; switch to HasColumnType("jsonb") for GIN indexing)
        builder.Property(e => e.Payload)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(e => e.MessageKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(OutboxStatus.Pending);

        builder.Property(e => e.RetryCount)
            .HasDefaultValue(0);

        builder.Property(e => e.LastError)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.ProcessedAt)
            .IsRequired(false);

        // Query index: the processor polls Status=Pending ordered by CreatedAt
        builder.HasIndex(e => new { e.Status, e.CreatedAt })
            .HasDatabaseName("IX_OutboxEvents_Status_CreatedAt");
    }
}
