using Gokt.Application.Interfaces;
using Gokt.Domain.Entities;
using Gokt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gokt.Infrastructure.Repositories;

public class TripMessageRepository(AppDbContext db) : ITripMessageRepository
{
    public async Task<IEnumerable<TripMessage>> GetByTripIdAsync(Guid tripId, CancellationToken ct = default) =>
        await db.TripMessages
            .Where(m => m.TripId == tripId)
            .OrderBy(m => m.SentAt)
            .ToListAsync(ct);

    public async Task AddAsync(TripMessage message, CancellationToken ct = default) =>
        await db.TripMessages.AddAsync(message, ct);
}
