using Gokt.Domain.Entities;
using Gokt.Domain.Enums;

namespace Gokt.Application.Interfaces;

public interface IDriverRepository
{
    Task<Driver?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Driver?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IEnumerable<Driver>> GetOnlineByVehicleTypeAsync(double latitude, double longitude, double radiusKm, VehicleType? vehicleType, CancellationToken ct = default);
    Task AddAsync(Driver driver, CancellationToken ct = default);
    Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<bool> ExistsByLicenseAsync(string licenseNumber, CancellationToken ct = default);
    Task<Driver?> GetByDriverCodeAsync(string driverCode, CancellationToken ct = default);
}
