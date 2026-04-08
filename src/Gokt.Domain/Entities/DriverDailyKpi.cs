namespace Gokt.Domain.Entities;

public class DriverDailyKpi
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid DriverId { get; private set; }
    public DateOnly RevenueDate { get; private set; }
    public decimal DailyRevenue { get; private set; }
    public bool IsQualified { get; private set; }
    public decimal AppliedRate { get; private set; }
    public decimal BaseAmount { get; private set; }
    public decimal CalculatedPay { get; private set; }
    public DateTime ProcessedAt { get; private set; } = DateTime.UtcNow;

    public Driver Driver { get; private set; } = default!;

    private DriverDailyKpi() { }

    public static DriverDailyKpi Create(
        Guid driverId,
        DateOnly revenueDate,
        decimal dailyRevenue,
        bool isQualified,
        decimal appliedRate,
        decimal baseAmount,
        decimal calculatedPay)
    {
        return new DriverDailyKpi
        {
            DriverId = driverId,
            RevenueDate = revenueDate,
            DailyRevenue = dailyRevenue,
            IsQualified = isQualified,
            AppliedRate = appliedRate,
            BaseAmount = baseAmount,
            CalculatedPay = calculatedPay,
            ProcessedAt = DateTime.UtcNow
        };
    }
}
