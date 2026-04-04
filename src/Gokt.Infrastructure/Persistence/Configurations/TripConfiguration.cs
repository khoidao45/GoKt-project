using Gokt.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gokt.Infrastructure.Persistence.Configurations;

public class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.ToTable("Trips");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.CancellationReason).HasMaxLength(500);
        builder.Property(t => t.FinalFare).HasPrecision(10, 2);
        builder.Property(t => t.ActualDistanceKm).HasPrecision(8, 2);
        builder.Property(t => t.CustomerRatingComment).HasMaxLength(500);
        builder.Property(t => t.DriverRatingComment).HasMaxLength(500);

        builder.HasOne(t => t.RideRequest)
            .WithOne(r => r.Trip)
            .HasForeignKey<Trip>(t => t.RideRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Driver)
            .WithMany(d => d.Trips)
            .HasForeignKey(t => t.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Vehicle)
            .WithMany()
            .HasForeignKey(t => t.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Customer)
            .WithMany()
            .HasForeignKey(t => t.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.RideRequestId).IsUnique(); // idempotency: one trip per ride
        builder.HasIndex(t => t.DriverId);
        builder.HasIndex(t => t.CustomerId);
        builder.HasIndex(t => t.Status);
    }
}
