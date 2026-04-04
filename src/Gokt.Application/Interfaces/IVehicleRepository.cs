using Gokt.Domain.Entities;

namespace Gokt.Application.Interfaces;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Vehicle>> GetByDriverIdAsync(Guid driverId, CancellationToken ct = default);
    Task<Vehicle?> GetActiveByDriverIdAsync(Guid driverId, CancellationToken ct = default);
    Task AddAsync(Vehicle vehicle, CancellationToken ct = default);
    Task<bool> ExistsByPlateNumberAsync(string plateNumber, CancellationToken ct = default);
}
