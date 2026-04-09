using Gokt.Application.Interfaces;
using Gokt.Domain.Entities;
using Gokt.Domain.Enums;
using Gokt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gokt.Infrastructure.Repositories;

public class DriverRepository(AppDbContext db) : IDriverRepository
{
    public Task<Driver?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Drivers
            .Include(d => d.Vehicles.Where(v => v.IsActive))
            .FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<Driver?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        db.Drivers
            .Include(d => d.Vehicles.Where(v => v.IsActive))
            .FirstOrDefaultAsync(d => d.UserId == userId, ct);

    public async Task<IEnumerable<Driver>> GetOnlineByVehicleTypeAsync(
        double latitude, double longitude, double radiusKm,
        VehicleType? vehicleType, CancellationToken ct = default)
    {
        var query = db.Drivers
            .Include(d => d.Vehicles.Where(v => v.IsActive))
            .Where(d => d.IsOnline && d.Status == DriverStatus.Active
                && d.CurrentLatitude.HasValue && d.CurrentLongitude.HasValue);

        if (vehicleType.HasValue)
            query = query.Where(d => d.Vehicles.Any(v => v.IsActive && v.VehicleType == vehicleType.Value));

        var candidates = await query.ToListAsync(ct);

        // Apply Haversine filter in memory (MVP — use PostGIS for production scale)
        return candidates.Where(d =>
            HaversineKm(latitude, longitude, d.CurrentLatitude!.Value, d.CurrentLongitude!.Value) <= radiusKm);
    }

    public Task AddAsync(Driver driver, CancellationToken ct = default) =>
        db.Drivers.AddAsync(driver, ct).AsTask();

    public Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        db.Drivers.AnyAsync(d => d.UserId == userId, ct);

    public Task<bool> ExistsByLicenseAsync(string licenseNumber, CancellationToken ct = default) =>
        db.Drivers.AnyAsync(d => d.LicenseNumber == licenseNumber, ct);

    public Task<Driver?> GetByDriverCodeAsync(string driverCode, CancellationToken ct = default) =>
        db.Drivers
            .Include(d => d.Vehicles.Where(v => v.IsActive))
            .FirstOrDefaultAsync(d => d.DriverCode == driverCode, ct);

    public async Task<IEnumerable<Driver>> GetAllAsync(int page, int pageSize, DriverStatus? status = null, CancellationToken ct = default)
    {
        var query = db.Drivers
            .Include(d => d.User).ThenInclude(u => u.Profile)
            .Include(d => d.Vehicles)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(d => d.Status == status.Value);

        return await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<int> CountAsync(DriverStatus? status = null, CancellationToken ct = default)
    {
        var query = db.Drivers.AsQueryable();
        if (status.HasValue)
            query = query.Where(d => d.Status == status.Value);
        return await query.CountAsync(ct);
    }

    public Task UpdateAsync(Driver driver, CancellationToken ct = default)
    {
        db.Drivers.Update(driver);
        return Task.CompletedTask;
    }

    private static double HaversineKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371;
        var dLat = ToRad(lat2 - lat1);
        var dLng = ToRad(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;
}
