using System.Text.Json;
using FluentValidation;
using Gokt.Application.DTOs;
using Gokt.Application.Events;
using Gokt.Application.Interfaces;
using Gokt.Domain.Entities;
using Gokt.Domain.Enums;
using Gokt.Domain.Exceptions;
using MediatR;

namespace Gokt.Application.Commands.Rides.CreateRideRequest;

public record CreateRideRequestCommand(
    Guid CustomerId,
    double PickupLatitude,
    double PickupLongitude,
    string PickupAddress,
    double DropoffLatitude,
    double DropoffLongitude,
    string DropoffAddress,
    VehicleType VehicleType,
    string? DriverCode = null
) : IRequest<RideRequestDto>;

public sealed class CreateRideRequestCommandValidator : AbstractValidator<CreateRideRequestCommand>
{
    public CreateRideRequestCommandValidator()
    {
        RuleFor(x => x.PickupLatitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.PickupLongitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.DropoffLatitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.DropoffLongitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.PickupAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DropoffAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DriverCode).MaximumLength(20).When(x => x.DriverCode is not null);
    }
}

public sealed class CreateRideRequestCommandHandler(
    IRideRequestRepository rideRequestRepository,
    IPricingService pricingService,
    IOutboxRepository outboxRepository,          // replaces IEventPublisher
    INotificationService notificationService,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateRideRequestCommand, RideRequestDto>
{
    public async Task<RideRequestDto> Handle(CreateRideRequestCommand cmd, CancellationToken ct)
    {
        var existing = await rideRequestRepository.GetActiveByCustomerIdAsync(cmd.CustomerId, ct);
        if (existing is not null)
            throw new ConflictException("You already have an active ride request.");

        var (fare, distanceKm) = await pricingService.CalculateAsync(
            cmd.PickupLatitude, cmd.PickupLongitude,
            cmd.DropoffLatitude, cmd.DropoffLongitude,
            cmd.VehicleType, ct);

        var request = RideRequest.Create(
            cmd.CustomerId,
            cmd.PickupLatitude, cmd.PickupLongitude, cmd.PickupAddress,
            cmd.DropoffLatitude, cmd.DropoffLongitude, cmd.DropoffAddress,
            cmd.VehicleType, fare, distanceKm);

        if (!string.IsNullOrWhiteSpace(cmd.DriverCode))
            request.AddDriverCode(cmd.DriverCode.ToUpper());

        await rideRequestRepository.AddAsync(request, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await notificationService.SendAsync(cmd.CustomerId,
            "Ride Requested", "Looking for a driver near you...",
            Domain.Enums.NotificationType.RideRequest, null, ct);

        // Transition to Searching
        request.StartSearching();

        // ── OUTBOX PATTERN ────────────────────────────────────────────────────
        // Build the event and stage both the status update and the outbox entry
        // in the SAME SaveChangesAsync call — guaranteed atomic with PostgreSQL.
        var rideEvent = new RideRequestedEvent(
            request.Id,
            request.CustomerId,
            request.PickupLatitude, request.PickupLongitude, request.PickupAddress,
            request.DropoffLatitude, request.DropoffLongitude, request.DropoffAddress,
            request.RequestedVehicleType.ToString(),
            request.EstimatedFare,
            request.EstimatedDistanceKm,
            request.DriverCode,
            request.ExpiresAt,
            DateTime.UtcNow);

        var outboxEvent = OutboxEvent.Create(
            type:       KafkaTopics.RideRequested,
            messageKey: request.Id.ToString(),
            payload:    JsonSerializer.Serialize(rideEvent));

        await outboxRepository.AddAsync(outboxEvent, ct);

        // Single SaveChanges: persists RideRequest status change + OutboxEvent atomically
        await unitOfWork.SaveChangesAsync(ct);
        // ─────────────────────────────────────────────────────────────────────

        return RideRequestDto.From(request);
    }
}
