using Gokt.Domain.Enums;

namespace Gokt.Domain.Entities;

public class PricingRule
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public VehicleType VehicleType { get; private set; }
    public decimal BaseFare { get; private set; }
    public decimal PerKmRate { get; private set; }
    public decimal PerMinuteRate { get; private set; }
    public decimal MinimumFare { get; private set; }
    public decimal SurgeMultiplier { get; private set; } = 1.0m;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private PricingRule() { }

    public static PricingRule CreateSeed(
        Guid id, VehicleType vehicleType,
        decimal baseFare, decimal perKmRate, decimal perMinuteRate, decimal minimumFare) =>
        new()
        {
            Id = id,
            VehicleType = vehicleType,
            BaseFare = baseFare,
            PerKmRate = perKmRate,
            PerMinuteRate = perMinuteRate,
            MinimumFare = minimumFare,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
}
