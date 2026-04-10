using Gokt.Domain.Entities;

namespace Gokt.Application.Interfaces;

public interface ITripMessageRepository
{
    Task<IEnumerable<TripMessage>> GetByTripIdAsync(Guid tripId, CancellationToken ct = default);
    Task AddAsync(TripMessage message, CancellationToken ct = default);
}
