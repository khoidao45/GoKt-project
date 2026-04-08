using Gokt.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gokt.Infrastructure.Persistence.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Make).IsRequired().HasMaxLength(50);
        builder.Property(v => v.Model).IsRequired().HasMaxLength(50);
        builder.Property(v => v.Color).IsRequired().HasMaxLength(30);
        builder.Property(v => v.PlateNumber).IsRequired().HasMaxLength(20);
        builder.Property(v => v.SeatCount).IsRequired();
        builder.Property(v => v.ImageUrl).HasMaxLength(500);
        builder.Property(v => v.VehicleType).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(v => v.Driver)
            .WithMany(d => d.Vehicles)
            .HasForeignKey(v => v.DriverId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(v => v.PlateNumber).IsUnique();
        builder.HasIndex(v => v.DriverId);
    }
}
