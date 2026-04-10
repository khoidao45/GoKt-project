namespace Gokt.Application.DTOs;

public record DriverDailyEarningsDto(
    DateOnly Date,
    decimal TripRevenue,
    decimal KpiPayout,
    bool KpiQualified,
    decimal KpiRate,
    decimal NetProfit,
    bool IsKpiFinalized
);
