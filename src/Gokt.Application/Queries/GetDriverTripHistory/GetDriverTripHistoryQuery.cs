using Gokt.Application.DTOs;
using Gokt.Application.Interfaces;
using Gokt.Domain.Exceptions;
using MediatR;

namespace Gokt.Application.Queries.GetDriverTripHistory;

public record GetDriverTripHistoryQuery(Guid UserId, int Page = 1, int PageSize = 20) : IRequest<IEnumerable<TripDto>>;

public sealed class GetDriverTripHistoryQueryHandler(
    IDriverRepository driverRepository,
    ITripRepository tripRepository) : IRequestHandler<GetDriverTripHistoryQuery, IEnumerable<TripDto>>
{
    public async Task<IEnumerable<TripDto>> Handle(GetDriverTripHistoryQuery query, CancellationToken ct)
    {
        var driver = await driverRepository.GetByUserIdAsync(query.UserId, ct)
            ?? throw new NotFoundException("Driver", query.UserId);

        var trips = await tripRepository.GetHistoryByDriverIdAsync(driver.Id, query.Page, query.PageSize, ct);
        return trips.Select(TripDto.From);
    }
}
