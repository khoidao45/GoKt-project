using Gokt.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gokt.Infrastructure.Persistence.Configurations;

public class TripMessageConfiguration : IEntityTypeConfiguration<TripMessage>
{
    public void Configure(EntityTypeBuilder<TripMessage> builder)
    {
        builder.ToTable("TripMessages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.SenderRole).IsRequired().HasMaxLength(10);
        builder.Property(m => m.Body).IsRequired().HasMaxLength(500);

        builder.HasIndex(m => new { m.TripId, m.SentAt });
    }
}
