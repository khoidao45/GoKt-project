using Gokt.Application.DTOs;
using Gokt.Application.Interfaces;
using Gokt.Domain.Exceptions;
using MediatR;

namespace Gokt.Application.Queries.GetDriverDailyEarnings;

public record GetDriverDailyEarningsQuery(Guid UserId) : IRequest<DriverDailyEarningsDto>;

public sealed class GetDriverDailyEarningsQueryHandler(
    IDriverRepository driverRepository,
    IDriverEarningsRepository earningsRepository)
    : IRequestHandler<GetDriverDailyEarningsQuery, DriverDailyEarningsDto>
{
    public async Task<DriverDailyEarningsDto> Handle(GetDriverDailyEarningsQuery query, CancellationToken ct)
    {
        var driver = await driverRepository.GetByUserIdAsync(query.UserId, ct)
            ?? throw new NotFoundException("Driver", query.UserId);

        var vnTimeZone = ResolveVnTimeZone();
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
        var localDate = DateOnly.FromDateTime(nowLocal.Date);

        var localStart = localDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart, vnTimeZone);
        var utcEnd = utcStart.AddDays(1);

        var data = await earningsRepository.GetDailyAsync(driver.Id, localDate, utcStart, utcEnd, ct);
        var netProfit = data.TripRevenue + data.KpiPayout;

        return new DriverDailyEarningsDto(
            localDate,
            data.TripRevenue,
            data.KpiPayout,
            data.KpiQualified,
            data.KpiRate,
            netProfit,
            data.KpiExists);
    }

    private static TimeZoneInfo ResolveVnTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
    }
}
