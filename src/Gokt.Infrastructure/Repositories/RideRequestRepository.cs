using Gokt.Application.Interfaces;
using Gokt.Domain.Entities;
using Gokt.Domain.Enums;
using Gokt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gokt.Infrastructure.Repositories;

public class RideRequestRepository(AppDbContext db) : IRideRequestRepository
{
    public Task<RideRequest?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.RideRequests
            .Include(r => r.Trip)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<RideRequest?> GetActiveByCustomerIdAsync(Guid customerId, CancellationToken ct = default) =>
        db.RideRequests.FirstOrDefaultAsync(
            r => r.CustomerId == customerId
              && (r.Status == RideStatus.Pending
                  || r.Status == RideStatus.Accepted
                  || r.Status == RideStatus.Searching), ct);

    public async Task<IEnumerable<RideRequest>> GetPendingByVehicleTypeAsync(VehicleType vehicleType, CancellationToken ct = default) =>
        await db.RideRequests
            .Where(r => r.Status == RideStatus.Pending
                     && r.RequestedVehicleType == vehicleType
                     && r.ExpiresAt > DateTime.UtcNow)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(ct);

    public Task AddAsync(RideRequest rideRequest, CancellationToken ct = default) =>
        db.RideRequests.AddAsync(rideRequest, ct).AsTask();

    public async Task<IEnumerable<RideRequest>> GetExpiredSearchingAsync(CancellationToken ct = default) =>
        await db.RideRequests
            .Where(r => r.Status == RideStatus.Searching && r.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(ct);
}
