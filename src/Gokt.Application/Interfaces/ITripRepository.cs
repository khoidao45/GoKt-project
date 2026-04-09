using Gokt.Domain.Entities;

namespace Gokt.Application.Interfaces;

public interface ITripRepository
{
    Task<Trip?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Trip?> GetActiveByDriverIdAsync(Guid driverId, CancellationToken ct = default);
    Task<Trip?> GetActiveByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task<IEnumerable<Trip>> GetHistoryByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<IEnumerable<Trip>> GetHistoryByDriverIdAsync(Guid driverId, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Trip trip, CancellationToken ct = default);
    Task<Trip?> GetByRideRequestIdAsync(Guid rideRequestId, CancellationToken ct = default);
    Task<IEnumerable<Trip>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
}
