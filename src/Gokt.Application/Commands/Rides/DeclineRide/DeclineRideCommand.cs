using Gokt.Application.Interfaces;
using Gokt.Domain.Enums;
using Gokt.Domain.Exceptions;
using MediatR;

namespace Gokt.Application.Commands.Rides.DeclineRide;

public record DeclineRideCommand(Guid UserId, Guid RideRequestId) : IRequest;

public sealed class DeclineRideCommandHandler(
    IDriverRepository driverRepository,
    IRideRequestRepository rideRequestRepository,
    ILocationService locationService) : IRequestHandler<DeclineRideCommand>
{
    private static readonly TimeSpan CooldownDuration = TimeSpan.FromSeconds(30);

    public async Task Handle(DeclineRideCommand cmd, CancellationToken ct)
    {
        var driver = await driverRepository.GetByUserIdAsync(cmd.UserId, ct)
            ?? throw new NotFoundException("Driver", cmd.UserId);

        var rideRequest = await rideRequestRepository.GetByIdAsync(cmd.RideRequestId, ct)
            ?? throw new NotFoundException("RideRequest", cmd.RideRequestId);

        if (rideRequest.Status is not (RideStatus.Searching or RideStatus.Pending))
            return; // Ride no longer active — silently ignore

        if (rideRequest.ExpiresAt < DateTime.UtcNow)
            return; // Already expired

        // Remove this driver from the Redis candidate set so they won't receive
        // RideTaken notifications or be re-notified in subsequent waves
        await locationService.RemoveDriverFromCandidatesAsync(rideRequest.Id, driver.Id, ct);

        // Apply cooldown so this driver is skipped for the next 30 seconds
        await locationService.SetDriverCooldownAsync(driver.Id, CooldownDuration, ct);
    }
}
