using Gokt.Domain.Entities;
using Gokt.Domain.Enums;

namespace Gokt.Application.Interfaces;

public interface IRideRequestRepository
{
    Task<RideRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<RideRequest?> GetActiveByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task<IEnumerable<RideRequest>> GetPendingByVehicleTypeAsync(VehicleType vehicleType, CancellationToken ct = default);
    Task AddAsync(RideRequest rideRequest, CancellationToken ct = default);
    Task<IEnumerable<RideRequest>> GetExpiredSearchingAsync(CancellationToken ct = default);
}
