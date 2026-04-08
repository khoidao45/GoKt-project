namespace Gokt.Infrastructure.Services.Compensation;

public static class DailyCompensationCalculator
{
    private const decimal RevenueThreshold = 500_000m;
    private const decimal BaseAmount = 300_000m;

    public static DailyCompensationResult Calculate(decimal dailyRevenue)
    {
        if (dailyRevenue < RevenueThreshold)
        {
            return new DailyCompensationResult(false, 0m, BaseAmount, 0m);
        }

        var rate = ResolveRate(dailyRevenue);
        var pay = BaseAmount + (dailyRevenue * rate);
        return new DailyCompensationResult(true, rate, BaseAmount, decimal.Round(pay, 0, MidpointRounding.AwayFromZero));
    }

    private static decimal ResolveRate(decimal revenue)
    {
        if (revenue >= 1_500_000m) return 0.30m;
        if (revenue >= 1_000_000m) return 0.25m;
        if (revenue >= 700_000m) return 0.20m;
        return 0.10m;
    }
}

public sealed record DailyCompensationResult(
    bool IsQualified,
    decimal AppliedRate,
    decimal BaseAmount,
    decimal DailyPay);
