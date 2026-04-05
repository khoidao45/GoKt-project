namespace Gokt.Application.Interfaces;

public interface ILocationService
{
    // Location + availability
    Task UpdateDriverLocationAsync(Guid driverId, double lat, double lng,
        bool isOnline, bool isBusy, string vehicleType, CancellationToken ct = default);

    Task RemoveDriverAsync(Guid driverId, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> GetNearbyAvailableDriversAsync(
        double lat, double lng, double radiusKm, string vehicleType, CancellationToken ct = default);

    Task MarkDriverBusyAsync(Guid driverId, CancellationToken ct = default);
    Task MarkDriverAvailableAsync(Guid driverId, CancellationToken ct = default);

    // Multi-connection SignalR (Redis SET per driver)
    Task AddDriverConnectionAsync(Guid driverId, string connectionId, CancellationToken ct = default);
    Task RemoveDriverConnectionAsync(Guid driverId, string connectionId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetDriverConnectionsAsync(Guid driverId, CancellationToken ct = default);

    // Distributed ride-lock (SET NX EX)
    Task<bool> TryAcquireRideLockAsync(Guid rideRequestId, Guid driverId,
        TimeSpan expiry, CancellationToken ct = default);
    Task ReleaseRideLockAsync(Guid rideRequestId, CancellationToken ct = default);
    Task<Guid?> GetRideLockHolderAsync(Guid rideRequestId, CancellationToken ct = default);

    // Wave-aware candidate tracking
    Task SetRideCandidatesAsync(Guid rideRequestId, IReadOnlyList<Guid> driverIds,
        TimeSpan expiry, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetRideCandidatesAsync(Guid rideRequestId, CancellationToken ct = default);
    Task RemoveDriverFromCandidatesAsync(Guid rideRequestId, Guid driverId, CancellationToken ct = default);

    // Driver cooldown after decline
    Task SetDriverCooldownAsync(Guid driverId, TimeSpan duration, CancellationToken ct = default);
    Task<bool> IsDriverInCooldownAsync(Guid driverId, CancellationToken ct = default);
}
