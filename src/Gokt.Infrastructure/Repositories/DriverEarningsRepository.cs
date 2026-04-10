using Gokt.Application.Interfaces;
using Gokt.Domain.Enums;
using Gokt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gokt.Infrastructure.Repositories;

public class DriverEarningsRepository(AppDbContext db) : IDriverEarningsRepository
{
    public async Task<DriverDailyEarningsData> GetDailyAsync(
        Guid driverId,
        DateOnly localDate,
        DateTime utcStartInclusive,
        DateTime utcEndExclusive,
        CancellationToken ct = default)
    {
        var tripRevenue = await db.Trips
            .AsNoTracking()
            .Where(t => t.DriverId == driverId
                        && t.Status == TripStatus.Completed
                        && t.CompletedAt.HasValue
                        && t.CompletedAt.Value >= utcStartInclusive
                        && t.CompletedAt.Value < utcEndExclusive)
            .SumAsync(t => t.FinalFare ?? 0m, ct);

        var dailyKpi = await db.DriverDailyKpis
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.DriverId == driverId && k.RevenueDate == localDate, ct);

        return new DriverDailyEarningsData(
            tripRevenue,
            dailyKpi?.CalculatedPay ?? 0m,
            dailyKpi?.IsQualified ?? false,
            dailyKpi?.AppliedRate ?? 0m,
            dailyKpi is not null);
    }
}
