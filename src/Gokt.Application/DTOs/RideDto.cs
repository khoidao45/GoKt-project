using Gokt.Domain.Entities;
using Gokt.Domain.Enums;

namespace Gokt.Application.DTOs;

public record VehicleDto(
    Guid Id,
    string Make,
    string Model,
    int Year,
    string Color,
    string PlateNumber,
    int SeatCount,
    string? ImageUrl,
    VehicleType VehicleType,
    bool IsVerified,
    DateTime? VerifiedAt
)
{
    public static VehicleDto From(Vehicle v) =>
        new(v.Id, v.Make, v.Model, v.Year, v.Color, v.PlateNumber, v.SeatCount, v.ImageUrl, v.VehicleType, v.IsVerified, v.VerifiedAt);
}

public record DriverDto(
    Guid Id,
    Guid UserId,
    string FullName,
    string? AvatarUrl,
    DriverStatus Status,
    decimal Rating,
    int TotalRides,
    bool IsOnline,
    double? Latitude,
    double? Longitude,
    List<VehicleDto> Vehicles
)
{
    public static DriverDto From(Driver d, string fullName, string? avatarUrl = null) =>
        new(d.Id, d.UserId, fullName, avatarUrl, d.Status, d.Rating, d.TotalRides,
            d.IsOnline, d.CurrentLatitude, d.CurrentLongitude,
            d.Vehicles.Select(VehicleDto.From).ToList());
}

public record RideRequestDto(
    Guid Id,
    Guid CustomerId,
    string PickupAddress,
    double PickupLatitude,
    double PickupLongitude,
    string DropoffAddress,
    double DropoffLatitude,
    double DropoffLongitude,
    VehicleType VehicleType,
    RideStatus Status,
    decimal EstimatedFare,
    decimal? EstimatedDistanceKm,
    DateTime CreatedAt,
    DateTime ExpiresAt
)
{
    public static RideRequestDto From(RideRequest r) =>
        new(r.Id, r.CustomerId, r.PickupAddress, r.PickupLatitude, r.PickupLongitude,
            r.DropoffAddress, r.DropoffLatitude, r.DropoffLongitude,
            r.RequestedVehicleType, r.Status, r.EstimatedFare, r.EstimatedDistanceKm,
            r.CreatedAt, r.ExpiresAt);
}

public record TripDto(
    Guid Id,
    Guid RideRequestId,
    Guid DriverId,
    Guid CustomerId,
    Guid VehicleId,
    TripStatus Status,
    string PickupAddress,
    string DropoffAddress,
    decimal? FinalFare,
    decimal? ActualDistanceKm,
    int? ActualDurationMinutes,
    DateTime AcceptedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    DateTime? CancelledAt,
    string? CancellationReason,
    int? CustomerRating,
    int? DriverRating,
    string? DriverName,
    string? DriverAvatarUrl,
    string? VehicleMake,
    string? VehicleModel,
    string? VehicleColor,
    string? VehiclePlateNumber,
    int? VehicleSeatCount,
    string? VehicleImageUrl
)
{
    public static TripDto From(Trip t) =>
        new(t.Id, t.RideRequestId, t.DriverId, t.CustomerId, t.VehicleId,
            t.Status,
            t.RideRequest?.PickupAddress ?? string.Empty,
            t.RideRequest?.DropoffAddress ?? string.Empty,
            t.FinalFare, t.ActualDistanceKm, t.ActualDurationMinutes,
            t.AcceptedAt, t.StartedAt, t.CompletedAt, t.CancelledAt,
            t.CancellationReason, t.CustomerRating, t.DriverRating,
            t.Driver?.User?.Profile?.FullName,
            t.Driver?.User?.Profile?.AvatarUrl,
            t.Vehicle?.Make,
            t.Vehicle?.Model,
            t.Vehicle?.Color,
            t.Vehicle?.PlateNumber,
            t.Vehicle?.SeatCount,
            t.Vehicle?.ImageUrl);
}

public record PriceEstimateDto(
    VehicleType VehicleType,
    decimal EstimatedFare,
    decimal EstimatedDistanceKm,
    string Currency = "USD"
);

public record NotificationDto(
    Guid Id,
    string Title,
    string Body,
    NotificationType Type,
    bool IsRead,
    DateTime CreatedAt
)
{
    public static NotificationDto From(Notification n) =>
        new(n.Id, n.Title, n.Body, n.Type, n.IsRead, n.CreatedAt);
}

public record ActiveRideDto(
    RideRequestDto? RideRequest,
    TripDto? Trip
);
