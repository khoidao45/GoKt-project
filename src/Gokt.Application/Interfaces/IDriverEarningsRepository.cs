namespace Gokt.Application.Interfaces;

public interface IDriverEarningsRepository
{
    Task<DriverDailyEarningsData> GetDailyAsync(
        Guid driverId,
        DateOnly localDate,
        DateTime utcStartInclusive,
        DateTime utcEndExclusive,
        CancellationToken ct = default);
}

public record DriverDailyEarningsData(
    decimal TripRevenue,
    decimal KpiPayout,
    bool KpiQualified,
    decimal KpiRate,
    bool KpiExists
);
