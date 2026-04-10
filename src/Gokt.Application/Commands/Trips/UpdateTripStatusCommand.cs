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

        string? notificationTitle = null;
        string? notificationBody = null;
        NotificationType? notificationType = null;

        switch (cmd.NewStatus)
        {
            case TripStatus.DriverEnRoute:
                trip.SetDriverEnRoute();
                break;
            case TripStatus.DriverArrived:
                trip.SetDriverArrived();
                notificationTitle = "Driver Arrived";
                notificationBody = "Your driver has arrived at the pickup location.";
                notificationType = NotificationType.DriverArriving;
                break;
            case TripStatus.InProgress:
                trip.Start();
                notificationTitle = "Trip Started";
                notificationBody = "Your trip is now in progress.";
                notificationType = NotificationType.TripStarted;
                break;
        }

        await unitOfWork.SaveChangesAsync(ct);

        if (notificationType.HasValue && notificationTitle is not null && notificationBody is not null)
        {
            await notificationService.SendAsync(
                trip.CustomerId,
                notificationTitle,
                notificationBody,
                notificationType.Value,
                null,
                ct);
        }

        return TripDto.From(trip);
    }
}
