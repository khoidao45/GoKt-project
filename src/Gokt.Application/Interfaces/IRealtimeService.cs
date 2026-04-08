namespace Gokt.Application.Interfaces;

public interface IRealtimeService
{
    Task NotifyDriverOfRideAsync(IReadOnlyList<string> connectionIds, RideOfferPayload payload, CancellationToken ct = default);
    Task NotifyDriverRideTakenAsync(IReadOnlyList<string> connectionIds, Guid rideRequestId, CancellationToken ct = default);
    Task NotifyCustomerDriverFoundAsync(Guid customerId, DriverFoundPayload payload, CancellationToken ct = default);
    Task NotifyCustomerNoDriverFoundAsync(Guid customerId, Guid rideRequestId, CancellationToken ct = default);
    Task BroadcastDriverLocationAsync(Guid customerId, DriverLocationPayload payload, CancellationToken ct = default);
    Task NotifyRideCancelledAsync(Guid targetUserId, Guid rideRequestId, string reason, CancellationToken ct = default);
}

public record RideOfferPayload(
    Guid RideRequestId,
    double PickupLat,
    double PickupLng,
    string PickupAddress,
    double DropoffLat,
    double DropoffLng,
    string DropoffAddress,
    string VehicleType,
    decimal EstimatedFare,
    decimal? EstimatedDistanceKm,
    DateTime ExpiresAt);

public record DriverFoundPayload(
    Guid TripId,
    Guid DriverId,
    string DriverName,
    string? DriverAvatarUrl,
    decimal Rating,
    string VehicleMake,
    string VehicleModel,
    string VehicleColor,
    string VehiclePlate,
    int VehicleSeatCount,
    string? VehicleImageUrl,
    double DriverLat,
    double DriverLng,
    string? DriverCode);

public record DriverLocationPayload(
    Guid DriverId,
    double Latitude,
    double Longitude,
    DateTime UpdatedAt);
