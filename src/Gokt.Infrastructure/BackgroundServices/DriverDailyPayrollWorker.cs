using Gokt.Domain.Entities;
using Gokt.Domain.Enums;
using Gokt.Infrastructure.Persistence;
using Gokt.Infrastructure.Services.Compensation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gokt.Infrastructure.BackgroundServices;

public class DriverDailyPayrollWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<DriverDailyPayrollWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);
    private static readonly TimeZoneInfo VnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("DriverDailyPayrollWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPreviousDayAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DriverDailyPayrollWorker failed while processing daily payroll");
            }

            await Task.Delay(Interval, stoppingToken);
        }

        logger.LogInformation("DriverDailyPayrollWorker stopped");
    }

    private async Task ProcessPreviousDayAsync(CancellationToken ct)
    {
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VnTimeZone);
        var targetLocalDate = DateOnly.FromDateTime(nowLocal.Date.AddDays(-1));

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var driverIds = await db.Drivers.Select(x => x.Id).ToListAsync(ct);
        if (driverIds.Count == 0) return;

        var processedDriverIds = await db.DriverDailyKpis
            .Where(x => x.RevenueDate == targetLocalDate)
            .Select(x => x.DriverId)
            .ToListAsync(ct);

        var pendingDriverIds = driverIds.Except(processedDriverIds).ToList();
        if (pendingDriverIds.Count == 0) return;

        var utcStart = LocalDateStartToUtc(targetLocalDate);
        var utcEnd = utcStart.AddDays(1);

        var revenues = await db.Trips
            .Where(x => pendingDriverIds.Contains(x.DriverId)
                        && x.Status == TripStatus.Completed
                        && x.CompletedAt.HasValue
                        && x.CompletedAt.Value >= utcStart
                        && x.CompletedAt.Value < utcEnd)
            .GroupBy(x => x.DriverId)
            .Select(g => new
            {
                DriverId = g.Key,
                DailyRevenue = g.Sum(x => x.FinalFare ?? 0m)
            })
            .ToDictionaryAsync(x => x.DriverId, x => x.DailyRevenue, ct);

        var wallets = await db.DriverWallets
            .Where(x => pendingDriverIds.Contains(x.DriverId))
            .ToDictionaryAsync(x => x.DriverId, ct);

        foreach (var driverId in pendingDriverIds)
        {
            var dailyRevenue = revenues.TryGetValue(driverId, out var value) ? value : 0m;
            var result = DailyCompensationCalculator.Calculate(dailyRevenue);

            if (!wallets.TryGetValue(driverId, out var wallet))
            {
                wallet = DriverWallet.Create(driverId);
                wallets[driverId] = wallet;
                db.DriverWallets.Add(wallet);
            }

            if (result.DailyPay > 0)
            {
                wallet.Credit(result.DailyPay);
            }

            db.DriverWalletTransactions.Add(DriverWalletTransaction.Create(
                wallet.Id,
                driverId,
                result.DailyPay > 0 ? WalletTransactionType.DailySalaryCredit : WalletTransactionType.DailyZeroResult,
                result.DailyPay,
                targetLocalDate,
                $"Daily KPI payout for {targetLocalDate:yyyy-MM-dd}"));

            db.DriverDailyKpis.Add(DriverDailyKpi.Create(
                driverId,
                targetLocalDate,
                decimal.Round(dailyRevenue, 0, MidpointRounding.AwayFromZero),
                result.IsQualified,
                result.AppliedRate,
                result.BaseAmount,
                result.DailyPay));
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "DriverDailyPayrollWorker processed date {Date} for {Count} driver(s)",
            targetLocalDate, pendingDriverIds.Count);
    }

    private static DateTime LocalDateStartToUtc(DateOnly localDate)
    {
        var localStart = localDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(localStart, VnTimeZone);
    }
}
