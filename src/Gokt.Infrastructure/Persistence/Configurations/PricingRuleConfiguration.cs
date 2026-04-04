using Gokt.Domain.Entities;
using Gokt.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gokt.Infrastructure.Persistence.Configurations;

public class PricingRuleConfiguration : IEntityTypeConfiguration<PricingRule>
{
    public void Configure(EntityTypeBuilder<PricingRule> builder)
    {
        builder.ToTable("PricingRules");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.VehicleType).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.BaseFare).HasPrecision(8, 2);
        builder.Property(p => p.PerKmRate).HasPrecision(6, 2);
        builder.Property(p => p.PerMinuteRate).HasPrecision(6, 2);
        builder.Property(p => p.MinimumFare).HasPrecision(8, 2);
        builder.Property(p => p.SurgeMultiplier).HasPrecision(4, 2);

        builder.HasIndex(p => p.VehicleType).IsUnique();

        builder.HasData(
            PricingRule.CreateSeed(
                new Guid("b1b1b1b1-0000-0000-0000-000000000001"),
                VehicleType.Economy, 1.50m, 0.80m, 0.15m, 3.00m),
            PricingRule.CreateSeed(
                new Guid("b1b1b1b1-0000-0000-0000-000000000002"),
                VehicleType.Comfort, 2.00m, 1.20m, 0.20m, 5.00m),
            PricingRule.CreateSeed(
                new Guid("b1b1b1b1-0000-0000-0000-000000000003"),
                VehicleType.Premium, 3.00m, 2.00m, 0.30m, 8.00m),
            PricingRule.CreateSeed(
                new Guid("b1b1b1b1-0000-0000-0000-000000000004"),
                VehicleType.XL, 2.50m, 1.50m, 0.25m, 6.00m)
        );
    }
}
