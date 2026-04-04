using Gokt.Application.Interfaces;
using Gokt.Domain.Entities;
using Gokt.Domain.Enums;
using Gokt.Domain.Exceptions;
using MediatR;

namespace Gokt.Application.Commands.Rides.CancelRideRequest;

public record CancelRideRequestCommand(Guid UserId, Guid RideRequestId, string? Reason) : IRequest;

public sealed class CancelRideRequestCommandHandler(
    IRideRequestRepository rideRequestRepository,
    ITripRepository tripRepository,
    IDriverRepository driverRepository,
    ILocationService locationService,
    IRealtimeService realtimeService,
    INotificationService notificationService,
    IUnitOfWork unitOfWork) : IRequestHandler<CancelRideRequestCommand>
{
    public async Task Handle(CancelRideRequestCommand cmd, CancellationToken ct)
    {
        var rideRequest = await rideRequestRepository.GetByIdAsync(cmd.RideRequestId, ct)
            ?? throw new NotFoundException("RideRequest", cmd.RideRequestId);

        var isCustomer = rideRequest.CustomerId == cmd.UserId;
        var driver = await driverRepository.GetByUserIdAsync(cmd.UserId, ct);
        var isDriver = driver is not null;

        if (!isCustomer && !isDriver)
            throw new ForbiddenException("You are not authorized to cancel this ride.");

        if (rideRequest.Status is RideStatus.Cancelled or RideStatus.Expired)
            throw new ConflictException("This ride request is already cancelled or expired.");

        // Release any distributed lock held for this ride
        await locationService.ReleaseRideLockAsync(rideRequest.Id, ct);

        // If there's a trip already created, cancel it and free the driver
        Trip? activeTrip = null;
        if (rideRequest.Status == RideStatus.Accepted)
        {
            activeTrip = await tripRepository.GetByRideRequestIdAsync(rideRequest.Id, ct);
            if (activeTrip is not null)
            {
                activeTrip.Cancel(cmd.Reason);
                var tripDriver = await driverRepository.GetByIdAsync(activeTrip.DriverId, ct);
                if (tripDriver is not null)
                {
                    tripDriver.ClearBusy();
                    await locationService.MarkDriverAvailableAsync(tripDriver.Id, ct);
                }
            }
        }

        rideRequest.Cancel();
        await unitOfWork.SaveChangesAsync(ct);

        // Real-time notifications
        if (isDriver && driver is not null)
        {
            await realtimeService.NotifyRideCancelledAsync(
                rideRequest.CustomerId, rideRequest.Id, cmd.Reason ?? "Driver cancelled", ct);
            _ = notificationService.SendAsync(rideRequest.CustomerId,
                "Ride Cancelled", "Your driver has cancelled the ride.",
                NotificationType.Cancelled, null, ct);
        }
        else if (isCustomer && activeTrip is not null)
        {
            await realtimeService.NotifyRideCancelledAsync(
                activeTrip.DriverId, rideRequest.Id, cmd.Reason ?? "Customer cancelled", ct);
            _ = notificationService.SendAsync(activeTrip.DriverId,
                "Ride Cancelled", "The customer has cancelled the ride.",
                NotificationType.Cancelled, null, ct);
        }
    }
}
