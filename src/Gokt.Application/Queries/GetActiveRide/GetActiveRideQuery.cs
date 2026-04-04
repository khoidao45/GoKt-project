using Gokt.Application.DTOs;
using Gokt.Application.Interfaces;
using MediatR;

namespace Gokt.Application.Queries.GetActiveRide;

public record GetActiveRideQuery(Guid UserId) : IRequest<ActiveRideDto>;

public sealed class GetActiveRideQueryHandler(
    IRideRequestRepository rideRequestRepository,
    ITripRepository tripRepository) : IRequestHandler<GetActiveRideQuery, ActiveRideDto>
{
    public async Task<ActiveRideDto> Handle(GetActiveRideQuery query, CancellationToken ct)
    {
        // Check for active trip first (takes precedence)
        var trip = await tripRepository.GetActiveByCustomerIdAsync(query.UserId, ct);
        if (trip is not null)
            return new ActiveRideDto(null, TripDto.From(trip));

        // Fall back to pending ride request
        var rideRequest = await rideRequestRepository.GetActiveByCustomerIdAsync(query.UserId, ct);
        if (rideRequest is not null)
            return new ActiveRideDto(RideRequestDto.From(rideRequest), null);

        return new ActiveRideDto(null, null);
    }
}
