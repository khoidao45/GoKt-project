namespace Gokt.Application.Events;

public record RideRequestedEvent(
    Guid RideRequestId,
    Guid CustomerId,
    double PickupLat,
    double PickupLng,
    string PickupAddress,
    double DropoffLat,
    double DropoffLng,
    string DropoffAddress,
    string VehicleType,
    decimal EstimatedFare,
    decimal? EstimatedDistanceKm,
    string? DriverCode,
    DateTime ExpiresAt,
    DateTime OccurredAt);
