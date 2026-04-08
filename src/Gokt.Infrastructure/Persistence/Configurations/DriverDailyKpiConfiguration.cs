using Gokt.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gokt.Infrastructure.Persistence.Configurations;

public class DriverDailyKpiConfiguration : IEntityTypeConfiguration<DriverDailyKpi>
{
    public void Configure(EntityTypeBuilder<DriverDailyKpi> builder)
    {
        builder.ToTable("DriverDailyKpis");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RevenueDate).HasColumnType("date");
        builder.Property(x => x.DailyRevenue).HasPrecision(18, 2);
        builder.Property(x => x.AppliedRate).HasPrecision(8, 4);
        builder.Property(x => x.BaseAmount).HasPrecision(18, 2);
        builder.Property(x => x.CalculatedPay).HasPrecision(18, 2);

        builder.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.DriverId, x.RevenueDate }).IsUnique();
        builder.HasIndex(x => x.RevenueDate);
    }
}
