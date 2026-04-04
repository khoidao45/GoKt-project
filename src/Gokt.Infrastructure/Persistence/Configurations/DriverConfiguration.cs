using Gokt.Domain.Entities;
using Gokt.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gokt.Infrastructure.Persistence.Configurations;

public class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("Drivers");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.LicenseNumber).IsRequired().HasMaxLength(50);
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.Rating).HasPrecision(3, 1);
        builder.Property(d => d.IsBusy).HasDefaultValue(false);
        builder.Property(d => d.DriverCode).HasMaxLength(20).IsRequired(false);

        builder.HasOne(d => d.User)
            .WithOne()
            .HasForeignKey<Driver>(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => d.UserId).IsUnique();
        builder.HasIndex(d => d.LicenseNumber).IsUnique();
        builder.HasIndex(d => d.IsOnline);
        builder.HasIndex(d => d.DriverCode).IsUnique().HasFilter("\"DriverCode\" IS NOT NULL");
        builder.HasIndex(d => new { d.IsOnline, d.IsBusy });
    }
}
