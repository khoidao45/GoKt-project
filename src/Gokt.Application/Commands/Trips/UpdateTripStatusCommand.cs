using FluentValidation;
using Gokt.Application.DTOs;
using Gokt.Application.Interfaces;
using Gokt.Domain.Enums;
using Gokt.Domain.Exceptions;
using MediatR;

namespace Gokt.Application.Commands.Trips.UpdateTripStatus;

public record UpdateTripStatusCommand(Guid UserId, Guid TripId, TripStatus NewStatus) : IRequest<TripDto>;

public sealed class UpdateTripStatusCommandValidator : AbstractValidator<UpdateTripStatusCommand>
{
    private static readonly TripStatus[] AllowedTransitions =
        [TripStatus.DriverEnRoute, TripStatus.DriverArrived, TripStatus.InProgress];

    public UpdateTripStatusCommandValidator()
    {
        RuleFor(x => x.NewStatus)
            .Must(s => AllowedTransitions.Contains(s))
            .WithMessage("Use CompleteTrip or CancelRide for terminal statuses.");
    }
}

public sealed class UpdateTripStatusCommandHandler(
    IDriverRepository driverRepository,
    ITripRepository tripRepository,
    INotificationService notificationService,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateTripStatusCommand, TripDto>
{
    public async Task<TripDto> Handle(UpdateTripStatusCommand cmd, CancellationToken ct)
    {
        var driver = await driverRepository.GetByUserIdAsync(cmd.UserId, ct)
            ?? throw new NotFoundException("Driver", cmd.UserId);

        var trip = await tripRepository.GetByIdAsync(cmd.TripId, ct)
            ?? throw new NotFoundException("Trip", cmd.TripId);

        if (trip.DriverId != driver.Id)
            throw new ForbiddenException("This trip does not belong to you.");

        switch (cmd.NewStatus)
        {
            case TripStatus.DriverEnRoute:
                trip.SetDriverEnRoute();
                break;
            case TripStatus.DriverArrived:
                trip.SetDriverArrived();
                _ = notificationService.SendAsync(trip.CustomerId,
                    "Driver Arrived", "Your driver has arrived at the pickup location.",
                    NotificationType.DriverArriving, null, ct);
                break;
            case TripStatus.InProgress:
                trip.Start();
                _ = notificationService.SendAsync(trip.CustomerId,
                    "Trip Started", "Your trip is now in progress.",
                    NotificationType.TripStarted, null, ct);
                break;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return TripDto.From(trip);
    }
}
