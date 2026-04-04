using FluentValidation;
using Gokt.Application.DTOs;
using Gokt.Application.Interfaces;
using Gokt.Domain.Enums;
using Gokt.Domain.Exceptions;
using MediatR;

namespace Gokt.Application.Commands.Trips.CompleteTrip;

public record CompleteTripCommand(Guid UserId, Guid TripId, decimal ActualDistanceKm) : IRequest<TripDto>;

public sealed class CompleteTripCommandValidator : AbstractValidator<CompleteTripCommand>
{
    public CompleteTripCommandValidator()
    {
        RuleFor(x => x.ActualDistanceKm).GreaterThan(0);
    }
}

public sealed class CompleteTripCommandHandler(
    IDriverRepository driverRepository,
    ITripRepository tripRepository,
    IPricingService pricingService,
    ILocationService locationService,
    INotificationService notificationService,
    IUnitOfWork unitOfWork) : IRequestHandler<CompleteTripCommand, TripDto>
{
    public async Task<TripDto> Handle(CompleteTripCommand cmd, CancellationToken ct)
    {
        var driver = await driverRepository.GetByUserIdAsync(cmd.UserId, ct)
            ?? throw new NotFoundException("Driver", cmd.UserId);

        var trip = await tripRepository.GetByIdAsync(cmd.TripId, ct)
            ?? throw new NotFoundException("Trip", cmd.TripId);

        if (trip.DriverId != driver.Id)
            throw new ForbiddenException("This trip does not belong to you.");

        if (trip.Status != TripStatus.InProgress)
            throw new ConflictException("Trip must be in progress to complete.");

        var rideRequest = trip.RideRequest;
        var (finalFare, _) = await pricingService.CalculateAsync(
            rideRequest.PickupLatitude, rideRequest.PickupLongitude,
            rideRequest.DropoffLatitude, rideRequest.DropoffLongitude,
            rideRequest.RequestedVehicleType, ct);

        trip.Complete(finalFare, cmd.ActualDistanceKm);
        driver.UpdateRating(driver.Rating); // keeps same rating, increments TotalRides
        driver.ClearBusy();

        await unitOfWork.SaveChangesAsync(ct);

        // Mark driver available in Redis hot-path
        await locationService.MarkDriverAvailableAsync(driver.Id, ct);

        _ = notificationService.SendAsync(trip.CustomerId,
            "Trip Completed", $"Your trip is complete. Fare: {finalFare:C}",
            NotificationType.TripCompleted, null, ct);

        return TripDto.From(trip);
    }
}
