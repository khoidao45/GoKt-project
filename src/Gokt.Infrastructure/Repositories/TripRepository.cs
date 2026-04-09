using Gokt.Application.Interfaces;
using Gokt.Domain.Entities;
using Gokt.Domain.Enums;
using Gokt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gokt.Infrastructure.Repositories;

public class TripRepository(AppDbContext db) : ITripRepository
{
    private IQueryable<Trip> WithIncludes() =>
        db.Trips
            .Include(t => t.RideRequest)
            .Include(t => t.Driver)
                .ThenInclude(d => d.User)
                    .ThenInclude(u => u.Profile)
            .Include(t => t.Vehicle);

    public Task<Trip?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        WithIncludes().FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Trip?> GetActiveByDriverIdAsync(Guid driverId, CancellationToken ct = default) =>
        WithIncludes().FirstOrDefaultAsync(
            t => t.DriverId == driverId && ActiveStatuses.Contains(t.Status), ct);

    public Task<Trip?> GetActiveByCustomerIdAsync(Guid customerId, CancellationToken ct = default) =>
        WithIncludes().FirstOrDefaultAsync(
            t => t.CustomerId == customerId && ActiveStatuses.Contains(t.Status), ct);

    public async Task<IEnumerable<Trip>> GetHistoryByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken ct = default) =>
        await WithIncludes()
            .Where(t => t.CustomerId == userId)
            .OrderByDescending(t => t.AcceptedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

    public async Task<IEnumerable<Trip>> GetHistoryByDriverIdAsync(Guid driverId, int page, int pageSize, CancellationToken ct = default) =>
        await WithIncludes()
            .Where(t => t.DriverId == driverId)
            .OrderByDescending(t => t.AcceptedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

    public Task AddAsync(Trip trip, CancellationToken ct = default) =>
        db.Trips.AddAsync(trip, ct).AsTask();

    public Task<Trip?> GetByRideRequestIdAsync(Guid rideRequestId, CancellationToken ct = default) =>
        WithIncludes().FirstOrDefaultAsync(t => t.RideRequestId == rideRequestId, ct);

    public async Task<IEnumerable<Trip>> GetAllAsync(int page, int pageSize, CancellationToken ct = default) =>
        await WithIncludes()
            .OrderByDescending(t => t.AcceptedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public Task<int> CountAsync(CancellationToken ct = default) =>
        db.Trips.CountAsync(ct);

    private static readonly TripStatus[] ActiveStatuses =
        [TripStatus.Accepted, TripStatus.DriverEnRoute, TripStatus.DriverArrived, TripStatus.InProgress];
}
