using Gokt.Application.DTOs;
using Gokt.Application.Interfaces;
using Gokt.Domain.Entities;
using Gokt.Domain.Enums;
using Gokt.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Gokt.Application.Commands.Rides.AcceptRideRequest;

public record AcceptRideRequestCommand(Guid UserId, Guid RideRequestId) : IRequest<TripDto>;

public sealed class AcceptRideRequestCommandHandler(
    IDriverRepository driverRepository,
    IVehicleRepository vehicleRepository,
    IRideRequestRepository rideRequestRepository,
    ITripRepository tripRepository,
    IUserRepository userRepository,
    ILocationService locationService,
    IRealtimeService realtimeService,
    INotificationService notificationService,
    IUnitOfWork unitOfWork,
    ILogger<AcceptRideRequestCommandHandler> logger) : IRequestHandler<AcceptRideRequestCommand, TripDto>
{
    public async Task<TripDto> Handle(AcceptRideRequestCommand cmd, CancellationToken ct)
    {
        var driver = await driverRepository.GetByUserIdAsync(cmd.UserId, ct)
            ?? throw new NotFoundException("Driver", cmd.UserId);

        if (!driver.IsOnline)
            throw new ForbiddenException("You must be online to accept rides.");

        if (driver.Status != DriverStatus.Active)
            throw new ForbiddenException("Your driver account is not active.");

        var existingTrip = await tripRepository.GetActiveByDriverIdAsync(driver.Id, ct);
        if (existingTrip is not null)
            throw new ConflictException("You already have an active trip.");

        var rideRequest = await rideRequestRepository.GetByIdAsync(cmd.RideRequestId, ct)
            ?? throw new NotFoundException("RideRequest", cmd.RideRequestId);

        if (rideRequest.Status is not (RideStatus.Searching or RideStatus.Pending))
            throw new ConflictException("This ride request is no longer available.");

        if (rideRequest.ExpiresAt < DateTime.UtcNow)
        {
            rideRequest.Expire();
            await unitOfWork.SaveChangesAsync(ct);
            throw new ConflictException("This ride request has expired.");
        }

        // Idempotency: if trip already exists for this ride, return it
        var duplicateTrip = await tripRepository.GetByRideRequestIdAsync(rideRequest.Id, ct);
        if (duplicateTrip is not null)
        {
            logger.LogInformation("AcceptRide idempotent: ride {RideId} already has trip {TripId}",
                cmd.RideRequestId, duplicateTrip.Id);
            return TripDto.From(duplicateTrip);
        }

        // Acquire distributed lock to prevent race condition
        var lockAcquired = await locationService.TryAcquireRideLockAsync(
            rideRequest.Id, driver.Id, TimeSpan.FromSeconds(30), ct);
        if (!lockAcquired)
        {
            logger.LogWarning("AcceptRide lock failed: ride {RideId} driver {DriverId}", cmd.RideRequestId, driver.Id);
            throw new ConflictException("This ride has already been accepted by another driver.");
        }

        logger.LogInformation("AcceptRide: driver {DriverId} accepted ride {RideId}", driver.Id, cmd.RideRequestId);

        var vehicle = await vehicleRepository.GetActiveByDriverIdAsync(driver.Id, ct)
            ?? throw new ForbiddenException("No active vehicle found. Please add a vehicle first.");

        rideRequest.Accept();
        driver.SetBusy();
        var trip = Trip.Create(rideRequest.Id, driver.Id, rideRequest.CustomerId, vehicle.Id);

        await tripRepository.AddAsync(trip, ct);
        await unitOfWork.SaveChangesAsync(ct);

        // Mark driver busy in Redis hot-path
        await locationService.MarkDriverBusyAsync(driver.Id, ct);

        // Build driver name for notification
        var driverUser = await userRepository.GetByIdAsync(driver.UserId, ct);
        var driverName = driverUser?.Profile is not null
            ? $"{driverUser.Profile.FirstName} {driverUser.Profile.LastName}".Trim()
            : "Driver";

        // Notify customer via SignalR
        await realtimeService.NotifyCustomerDriverFoundAsync(rideRequest.CustomerId,
            new DriverFoundPayload(
                trip.Id,
                driver.Id,
                driverName,
                driverUser?.Profile?.AvatarUrl,
                driver.Rating,
                vehicle.Make,
                vehicle.Model,
                vehicle.Color,
                vehicle.PlateNumber,
                vehicle.SeatCount,
                vehicle.ImageUrl,
                driver.CurrentLatitude ?? 0,
                driver.CurrentLongitude ?? 0,
                driver.DriverCode),
            ct);

        // DB push notification
        _ = notificationService.SendAsync(rideRequest.CustomerId,
            "Driver Found!", "A driver has accepted your ride request.",
            NotificationType.RideAccepted, null, ct);

        // Notify losing candidate drivers (fire-and-forget)
        _ = Task.Run(async () =>
        {
            try
            {
                var candidates = await locationService.GetRideCandidatesAsync(cmd.RideRequestId);
                foreach (var candidateId in candidates.Where(id => id != driver.Id))
                {
                    var connIds = await locationService.GetDriverConnectionsAsync(candidateId);
                    if (connIds.Count > 0)
                        await realtimeService.NotifyDriverRideTakenAsync(connIds, cmd.RideRequestId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error notifying losing candidates for ride {RideId}", cmd.RideRequestId);
            }
        }, CancellationToken.None);

        var result = await tripRepository.GetByIdAsync(trip.Id, ct) ?? trip;
        return TripDto.From(result);
    }
}
