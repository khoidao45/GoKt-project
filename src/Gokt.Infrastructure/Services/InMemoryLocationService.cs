using System.Collections.Concurrent;
using Gokt.Application.Interfaces;

namespace Gokt.Infrastructure.Services;

public class InMemoryLocationService : ILocationService
{
    private readonly ConcurrentDictionary<Guid, DriverStatus> _drivers = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _connections = new();
    private readonly ConcurrentDictionary<Guid, LockEntry> _rideLocks = new();
    private readonly ConcurrentDictionary<Guid, CandidateEntry> _rideCandidates = new();
    private readonly ConcurrentDictionary<Guid, DateTimeOffset> _cooldowns = new();

    public Task UpdateDriverLocationAsync(Guid driverId, double lat, double lng, bool isOnline, bool isBusy, string vehicleType, CancellationToken ct = default)
    {
        _drivers[driverId] = new DriverStatus(lat, lng, isOnline, isBusy, vehicleType, DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }

    public Task RemoveDriverAsync(Guid driverId, CancellationToken ct = default)
    {
        _drivers.TryRemove(driverId, out _);
        _connections.TryRemove(driverId, out _);
        _cooldowns.TryRemove(driverId, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Guid>> GetNearbyAvailableDriversAsync(double lat, double lng, double radiusKm, string vehicleType, CancellationToken ct = default)
    {
        var list = _drivers
            .Where(kv => kv.Value.IsOnline && !kv.Value.IsBusy &&
                         string.Equals(kv.Value.VehicleType, vehicleType, StringComparison.OrdinalIgnoreCase) &&
                         DistanceKm(lat, lng, kv.Value.Latitude, kv.Value.Longitude) <= radiusKm)
            .OrderBy(kv => DistanceKm(lat, lng, kv.Value.Latitude, kv.Value.Longitude))
            .Select(kv => kv.Key)
            .ToList();

        return Task.FromResult((IReadOnlyList<Guid>)list);
    }

    public Task MarkDriverBusyAsync(Guid driverId, CancellationToken ct = default)
    {
        if (_drivers.TryGetValue(driverId, out var d)) _drivers[driverId] = d with { IsBusy = true, UpdatedAt = DateTimeOffset.UtcNow };
        return Task.CompletedTask;
    }

    public Task MarkDriverAvailableAsync(Guid driverId, CancellationToken ct = default)
    {
        if (_drivers.TryGetValue(driverId, out var d)) _drivers[driverId] = d with { IsBusy = false, UpdatedAt = DateTimeOffset.UtcNow };
        return Task.CompletedTask;
    }

    public Task AddDriverConnectionAsync(Guid driverId, string connectionId, CancellationToken ct = default)
    {
        var bag = _connections.GetOrAdd(driverId, _ => new ConcurrentDictionary<string, byte>());
        bag[connectionId] = 0;
        return Task.CompletedTask;
    }

    public Task RemoveDriverConnectionAsync(Guid driverId, string connectionId, CancellationToken ct = default)
    {
        if (_connections.TryGetValue(driverId, out var bag)) bag.TryRemove(connectionId, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetDriverConnectionsAsync(Guid driverId, CancellationToken ct = default)
    {
        if (!_connections.TryGetValue(driverId, out var bag)) return Task.FromResult((IReadOnlyList<string>)Array.Empty<string>());
        return Task.FromResult((IReadOnlyList<string>)bag.Keys.ToList());
    }

    public Task<bool> TryAcquireRideLockAsync(Guid rideRequestId, Guid driverId, TimeSpan expiry, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        _rideLocks.AddOrUpdate(
            rideRequestId,
            _ => new LockEntry(driverId, now.Add(expiry)),
            (_, existing) => existing.ExpiresAt <= now ? new LockEntry(driverId, now.Add(expiry)) : existing);

        var acquired = _rideLocks.TryGetValue(rideRequestId, out var lockEntry) && lockEntry.DriverId == driverId;
        return Task.FromResult(acquired);
    }

    public Task ReleaseRideLockAsync(Guid rideRequestId, CancellationToken ct = default)
    {
        _rideLocks.TryRemove(rideRequestId, out _);
        return Task.CompletedTask;
    }

    public Task<Guid?> GetRideLockHolderAsync(Guid rideRequestId, CancellationToken ct = default)
    {
        if (!_rideLocks.TryGetValue(rideRequestId, out var lockEntry)) return Task.FromResult<Guid?>(null);
        if (lockEntry.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _rideLocks.TryRemove(rideRequestId, out _);
            return Task.FromResult<Guid?>(null);
        }
        return Task.FromResult<Guid?>(lockEntry.DriverId);
    }

    public Task SetRideCandidatesAsync(Guid rideRequestId, IReadOnlyList<Guid> driverIds, TimeSpan expiry, CancellationToken ct = default)
    {
        _rideCandidates[rideRequestId] = new CandidateEntry(driverIds.ToHashSet(), DateTimeOffset.UtcNow.Add(expiry));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Guid>> GetRideCandidatesAsync(Guid rideRequestId, CancellationToken ct = default)
    {
        if (!_rideCandidates.TryGetValue(rideRequestId, out var candidates)) return Task.FromResult((IReadOnlyList<Guid>)Array.Empty<Guid>());
        if (candidates.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _rideCandidates.TryRemove(rideRequestId, out _);
            return Task.FromResult((IReadOnlyList<Guid>)Array.Empty<Guid>());
        }
        return Task.FromResult((IReadOnlyList<Guid>)candidates.DriverIds.ToList());
    }

    public Task RemoveDriverFromCandidatesAsync(Guid rideRequestId, Guid driverId, CancellationToken ct = default)
    {
        if (_rideCandidates.TryGetValue(rideRequestId, out var candidates))
        {
            candidates.DriverIds.Remove(driverId);
        }
        return Task.CompletedTask;
    }

    public Task SetDriverCooldownAsync(Guid driverId, TimeSpan duration, CancellationToken ct = default)
    {
        _cooldowns[driverId] = DateTimeOffset.UtcNow.Add(duration);
        return Task.CompletedTask;
    }

    public Task<bool> IsDriverInCooldownAsync(Guid driverId, CancellationToken ct = default)
    {
        if (!_cooldowns.TryGetValue(driverId, out var expiresAt)) return Task.FromResult(false);
        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            _cooldowns.TryRemove(driverId, out _);
            return Task.FromResult(false);
        }
        return Task.FromResult(true);
    }

    private static double DistanceKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double r = 6371;
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return r * c;
    }

    private static double ToRadians(double deg) => deg * (Math.PI / 180);

    private record DriverStatus(double Latitude, double Longitude, bool IsOnline, bool IsBusy, string VehicleType, DateTimeOffset UpdatedAt);
    private record LockEntry(Guid DriverId, DateTimeOffset ExpiresAt);
    private record CandidateEntry(HashSet<Guid> DriverIds, DateTimeOffset ExpiresAt);
}
