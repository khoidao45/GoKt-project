using Gokt.Application.DTOs;
using Gokt.Domain.Exceptions;
using Gokt.Application.Interfaces;
using MediatR;

namespace Gokt.Application.Queries.GetTripMessages;

public record GetTripMessagesQuery(Guid UserId, Guid TripId) : IRequest<IEnumerable<TripMessageDto>>;

public sealed class GetTripMessagesQueryHandler(
    ITripRepository tripRepository,
    IDriverRepository driverRepository,
    ITripMessageRepository messageRepository)
    : IRequestHandler<GetTripMessagesQuery, IEnumerable<TripMessageDto>>
{
    public async Task<IEnumerable<TripMessageDto>> Handle(GetTripMessagesQuery query, CancellationToken ct)
    {
        var trip = await tripRepository.GetByIdAsync(query.TripId, ct)
            ?? throw new NotFoundException("Trip", query.TripId);

        // Verify caller is rider or driver
        bool isRider = trip.CustomerId == query.UserId;
        bool isDriver = false;
        if (!isRider)
        {
            var driver = await driverRepository.GetByUserIdAsync(query.UserId, ct);
            isDriver = driver is not null && trip.DriverId == driver.Id;
        }

        if (!isRider && !isDriver)
            throw new ForbiddenException("You are not a participant of this trip.");

        var messages = await messageRepository.GetByTripIdAsync(query.TripId, ct);
        return messages.Select(m => new TripMessageDto(m.Id, m.TripId, m.SenderId, m.SenderRole, m.Body, m.SentAt));
    }
}
