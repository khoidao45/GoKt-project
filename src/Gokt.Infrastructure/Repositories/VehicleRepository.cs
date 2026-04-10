using Gokt.Application.Interfaces;
using Gokt.Domain.Entities;
using Gokt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gokt.Infrastructure.Repositories;

public class VehicleRepository(AppDbContext db) : IVehicleRepository
{
    public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Vehicles.FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<IEnumerable<Vehicle>> GetByDriverIdAsync(Guid driverId, CancellationToken ct = default) =>
        await db.Vehicles.Where(v => v.DriverId == driverId).ToListAsync(ct);

    public Task<Vehicle?> GetActiveByDriverIdAsync(Guid driverId, CancellationToken ct = default) =>
        db.Vehicles.FirstOrDefaultAsync(v => v.DriverId == driverId && v.IsActive, ct);

    public Task AddAsync(Vehicle vehicle, CancellationToken ct = default) =>
        db.Vehicles.AddAsync(vehicle, ct).AsTask();

    public Task<bool> ExistsByPlateNumberAsync(string plateNumber, CancellationToken ct = default) =>
        db.Vehicles.AnyAsync(v => v.PlateNumber == plateNumber.ToUpperInvariant(), ct);

    public Task<bool> ExistsByPlateNumberExceptAsync(string plateNumber, Guid vehicleId, CancellationToken ct = default) =>
        db.Vehicles.AnyAsync(v => v.Id != vehicleId && v.PlateNumber == plateNumber.ToUpperInvariant(), ct);

    public Task UpdateAsync(Vehicle vehicle, CancellationToken ct = default)
    {
        db.Vehicles.Update(vehicle);
        return Task.CompletedTask;
    }
}
