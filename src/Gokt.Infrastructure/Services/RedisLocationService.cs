using System.Text.Json;
using Gokt.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Gokt.Infrastructure.Services;

public class RedisLocationService(
    IConnectionMultiplexer redis,
    ILogger<RedisLocationService> logger) : ILocationService
{
    private const string GeoKey = "gokt:driver:location";
    private static string StatusKey(Guid driverId) => $"gokt:driver:status:{driverId}";
    private static string ConnectionsKey(Guid driverId) => $"gokt:driver:connections:{driverId}";
    private static string LockKey(Guid rideRequestId) => $"gokt:ride:lock:{rideRequestId}";
    private static string CandidatesKey(Guid rideRequestId) => $"gokt:ride:candidates:{rideRequestId}";
    private static string CooldownKey(Guid driverId) => $"gokt:driver:cooldown:{driverId}";

    private static readonly TimeSpan StatusTtl = TimeSpan.FromHours(2);
    private static readonly TimeSpan ConnectionTtl = TimeSpan.FromHours(2);

    // ── Location + availability ────────────────────────────────────────────────

    public async Task UpdateDriverLocationAsync(Guid driverId, double lat, double lng,
        bool isOnline, bool isBusy, string vehicleType, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        var entry = new DriverStatusEntry(isOnline, isBusy, vehicleType, DateTime.UtcNow);
        var statusJson = JsonSerializer.Serialize(entry);

        await db.GeoAddAsync(GeoKey, new GeoEntry(lng, lat, driverId.ToString()));
        await db.StringSetAsync(StatusKey(driverId), statusJson, StatusTtl);

        logger.LogDebug("Updated location for driver {DriverId}: ({Lat},{Lng}) online={IsOnline} busy={IsBusy}",
            driverId, lat, lng, isOnline, isBusy);
    }

    public async Task RemoveDriverAsync(Guid driverId, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        // GEO sets are stored as sorted sets — use SortedSetRemoveAsync to remove a member
        await db.SortedSetRemoveAsync(GeoKey, driverId.ToString());
        await db.KeyDeleteAsync(StatusKey(driverId));
        logger.LogInformation("Driver {DriverId} removed from location index", driverId);
    }

    public async Task<IReadOnlyList<Guid>> GetNearbyAvailableDriversAsync(
        double lat, double lng, double radiusKm, string vehicleType, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();

        // GeoSearch by radius from coordinates
        var results = await db.GeoSearchAsync(
            GeoKey,
            lng,
            lat,
            new GeoSearchCircle(radiusKm, GeoUnit.Kilometers),
            count: 50,
            demandClosest: true,
            order: Order.Ascending);

        var available = new List<Guid>();
        foreach (var r in results)
        {
            if (!Guid.TryParse(r.Member.ToString(), out var driverId)) continue;

            var statusJson = await db.StringGetAsync(StatusKey(driverId));
            if (statusJson.IsNullOrEmpty) continue;

            var status = JsonSerializer.Deserialize<DriverStatusEntry>(statusJson!);
            if (status is { IsOnline: true, IsBusy: false }
                && string.Equals(status.VehicleType, vehicleType, StringComparison.OrdinalIgnoreCase))
            {
                available.Add(driverId);
            }
        }

        return available;
    }

    public async Task MarkDriverBusyAsync(Guid driverId, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        var statusJson = await db.StringGetAsync(StatusKey(driverId));
        var entry = statusJson.IsNullOrEmpty
            ? new DriverStatusEntry(true, true, "Economy", DateTime.UtcNow)
            : JsonSerializer.Deserialize<DriverStatusEntry>(statusJson!)! with { IsBusy = true, UpdatedAt = DateTime.UtcNow };

        await db.StringSetAsync(StatusKey(driverId), JsonSerializer.Serialize(entry), StatusTtl);
        logger.LogInformation("Driver {DriverId} marked busy in Redis", driverId);
    }

    public async Task MarkDriverAvailableAsync(Guid driverId, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        var statusJson = await db.StringGetAsync(StatusKey(driverId));
        if (statusJson.IsNullOrEmpty) return;

        var entry = JsonSerializer.Deserialize<DriverStatusEntry>(statusJson!)! with
        {
            IsBusy = false,
            UpdatedAt = DateTime.UtcNow
        };
        await db.StringSetAsync(StatusKey(driverId), JsonSerializer.Serialize(entry), StatusTtl);
        logger.LogInformation("Driver {DriverId} marked available in Redis", driverId);
    }

    // ── Multi-connection (Redis SET) ───────────────────────────────────────────

    public async Task AddDriverConnectionAsync(Guid driverId, string connectionId, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        var key = ConnectionsKey(driverId);
        await db.SetAddAsync(key, connectionId);
        await db.KeyExpireAsync(key, ConnectionTtl);
        logger.LogInformation("Driver {DriverId} added connection {ConnectionId}", driverId, connectionId);
    }

    public async Task RemoveDriverConnectionAsync(Guid driverId, string connectionId, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        await db.SetRemoveAsync(ConnectionsKey(driverId), connectionId);
        logger.LogInformation("Driver {DriverId} removed connection {ConnectionId}", driverId, connectionId);
    }

    public async Task<IReadOnlyList<string>> GetDriverConnectionsAsync(Guid driverId, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        var members = await db.SetMembersAsync(ConnectionsKey(driverId));
        return members.Select(m => m.ToString()).Where(s => !string.IsNullOrEmpty(s)).ToList();
    }

    // ── Ride lock (SET NX EX) ─────────────────────────────────────────────────

    public async Task<bool> TryAcquireRideLockAsync(Guid rideRequestId, Guid driverId,
        TimeSpan expiry, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        var acquired = await db.StringSetAsync(
            LockKey(rideRequestId),
            driverId.ToString(),
            expiry,
            When.NotExists);

        if (acquired)
            logger.LogInformation("Ride lock acquired: ride={RideRequestId} driver={DriverId}", rideRequestId, driverId);
        else
            logger.LogWarning("Ride lock FAILED (already held): ride={RideRequestId} driver={DriverId}", rideRequestId, driverId);

        return acquired;
    }

    public async Task ReleaseRideLockAsync(Guid rideRequestId, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        await db.KeyDeleteAsync(LockKey(rideRequestId));
    }

    public async Task<Guid?> GetRideLockHolderAsync(Guid rideRequestId, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        var value = await db.StringGetAsync(LockKey(rideRequestId));
        return value.HasValue && Guid.TryParse(value, out var id) ? id : null;
    }

    // ── Wave-aware candidate tracking ─────────────────────────────────────────

    public async Task SetRideCandidatesAsync(Guid rideRequestId, IReadOnlyList<Guid> driverIds,
        TimeSpan expiry, CancellationToken ct = default)
    {
        if (driverIds.Count == 0) return;
        var db = redis.GetDatabase();
        var key = CandidatesKey(rideRequestId);
        await db.SetAddAsync(key, driverIds.Select(id => (RedisValue)id.ToString()).ToArray());
        await db.KeyExpireAsync(key, expiry);
    }

    public async Task<IReadOnlyList<Guid>> GetRideCandidatesAsync(Guid rideRequestId, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        var members = await db.SetMembersAsync(CandidatesKey(rideRequestId));
        return members
            .Select(m => m.ToString())
            .Where(s => Guid.TryParse(s, out _))
            .Select(Guid.Parse)
            .ToList();
    }

    public async Task RemoveDriverFromCandidatesAsync(Guid rideRequestId, Guid driverId, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        await db.SetRemoveAsync(CandidatesKey(rideRequestId), driverId.ToString());
    }

    // ── Driver cooldown (post-decline) ────────────────────────────────────────

    public async Task SetDriverCooldownAsync(Guid driverId, TimeSpan duration, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        await db.StringSetAsync(CooldownKey(driverId), "1", duration);
        logger.LogInformation("Driver {DriverId} placed in cooldown for {Seconds}s", driverId, duration.TotalSeconds);
    }

    public async Task<bool> IsDriverInCooldownAsync(Guid driverId, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        return await db.KeyExistsAsync(CooldownKey(driverId));
    }

    // ── Internal DTO ──────────────────────────────────────────────────────────

    private record DriverStatusEntry(bool IsOnline, bool IsBusy, string VehicleType, DateTime UpdatedAt);
}
