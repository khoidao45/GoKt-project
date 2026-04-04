using Gokt.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gokt.Infrastructure.Persistence.Configurations;

public class RideRequestConfiguration : IEntityTypeConfiguration<RideRequest>
{
    public void Configure(EntityTypeBuilder<RideRequest> builder)
    {
        builder.ToTable("RideRequests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.PickupAddress).IsRequired().HasMaxLength(500);
        builder.Property(r => r.DropoffAddress).IsRequired().HasMaxLength(500);
        builder.Property(r => r.RequestedVehicleType).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.EstimatedFare).HasPrecision(10, 2);
        builder.Property(r => r.EstimatedDistanceKm).HasPrecision(8, 2);
        builder.Property(r => r.DriverCode).HasMaxLength(20).IsRequired(false);

        builder.HasOne(r => r.Customer)
            .WithMany()
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.CustomerId);
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.CreatedAt);
    }
}
