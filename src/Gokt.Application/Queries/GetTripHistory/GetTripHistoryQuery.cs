using Gokt.Application.DTOs;
using Gokt.Application.Interfaces;
using MediatR;

namespace Gokt.Application.Queries.GetTripHistory;

public record GetTripHistoryQuery(Guid UserId, int Page = 1, int PageSize = 20) : IRequest<IEnumerable<TripDto>>;

public sealed class GetTripHistoryQueryHandler(ITripRepository tripRepository)
    : IRequestHandler<GetTripHistoryQuery, IEnumerable<TripDto>>
{
    public async Task<IEnumerable<TripDto>> Handle(GetTripHistoryQuery query, CancellationToken ct)
    {
        var trips = await tripRepository.GetHistoryByUserIdAsync(query.UserId, query.Page, query.PageSize, ct);
        return trips.Select(TripDto.From);
    }
}
