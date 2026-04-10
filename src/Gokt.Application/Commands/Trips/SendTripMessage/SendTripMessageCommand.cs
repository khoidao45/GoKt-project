using FluentValidation;
using Gokt.Application.DTOs;
using Gokt.Domain.Exceptions;
using Gokt.Application.Interfaces;
using Gokt.Domain.Entities;
using Gokt.Domain.Enums;
using MediatR;

namespace Gokt.Application.Commands.Trips.SendTripMessage;

public record SendTripMessageCommand(Guid UserId, Guid TripId, string Body) : IRequest<TripMessageDto>;

public sealed class SendTripMessageCommandValidator : AbstractValidator<SendTripMessageCommand>
{
    public SendTripMessageCommandValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MaximumLength(500);
    }
}

public sealed class SendTripMessageCommandHandler(
    ITripRepository tripRepository,
    IDriverRepository driverRepository,
    ITripMessageRepository messageRepository,
    IRealtimeService realtimeService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SendTripMessageCommand, TripMessageDto>
{
    public async Task<TripMessageDto> Handle(SendTripMessageCommand cmd, CancellationToken ct)
    {
        var trip = await tripRepository.GetByIdAsync(cmd.TripId, ct)
            ?? throw new NotFoundException("Trip", cmd.TripId);

        // Determine sender role
        string senderRole;
        if (trip.CustomerId == cmd.UserId)
        {
            senderRole = "Rider";
        }
        else
        {
            var driver = await driverRepository.GetByUserIdAsync(cmd.UserId, ct);
            if (driver is null || trip.DriverId != driver.Id)
                throw new ForbiddenException("You are not a participant of this trip.");
            senderRole = "Driver";
        }

        // Only allow messaging on active trips
        if (trip.Status is TripStatus.Completed or TripStatus.Cancelled)
            throw new ConflictException("Cannot send messages on a completed or cancelled trip.");

        var message = TripMessage.Create(cmd.TripId, cmd.UserId, senderRole, cmd.Body);
        await messageRepository.AddAsync(message, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var dto = new TripMessageDto(message.Id, message.TripId, message.SenderId,
            message.SenderRole, message.Body, message.SentAt);

        // Push to both participants via SignalR
        var driver2 = await driverRepository.GetByIdAsync(trip.DriverId, ct);
        if (driver2 is not null)
            _ = realtimeService.SendTripMessageAsync(driver2.Id, trip.CustomerId, dto, ct);

        return dto;
    }
}
