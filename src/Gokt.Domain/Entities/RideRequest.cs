using Gokt.Domain.Enums;

namespace Gokt.Domain.Entities;

public class RideRequest
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CustomerId { get; private set; }
    public double PickupLatitude { get; private set; }
    public double PickupLongitude { get; private set; }
    public string PickupAddress { get; private set; } = default!;
    public double DropoffLatitude { get; private set; }
    public double DropoffLongitude { get; private set; }
    public string DropoffAddress { get; private set; } = default!;
    public VehicleType RequestedVehicleType { get; private set; }
    public RideStatus Status { get; private set; } = RideStatus.Pending;
    public decimal EstimatedFare { get; private set; }
    public decimal? EstimatedDistanceKm { get; private set; }
    public string? DriverCode { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; private set; }

    public User Customer { get; private set; } = default!;
    public Trip? Trip { get; private set; }

    private RideRequest() { }

    public static RideRequest Create(
        Guid customerId,
        double pickupLat, double pickupLng, string pickupAddress,
        double dropoffLat, double dropoffLng, string dropoffAddress,
        VehicleType vehicleType,
        decimal estimatedFare,
        decimal? estimatedDistanceKm = null) =>
        new()
        {
            CustomerId = customerId,
            PickupLatitude = pickupLat,
            PickupLongitude = pickupLng,
            PickupAddress = pickupAddress,
            DropoffLatitude = dropoffLat,
            DropoffLongitude = dropoffLng,
            DropoffAddress = dropoffAddress,
            RequestedVehicleType = vehicleType,
            EstimatedFare = estimatedFare,
            EstimatedDistanceKm = estimatedDistanceKm,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

    public void StartSearching() => Status = RideStatus.Searching;
    public void Accept() => Status = RideStatus.Accepted;
    public void Cancel() => Status = RideStatus.Cancelled;
    public void Expire() => Status = RideStatus.Expired;

    public void AddDriverCode(string driverCode) => DriverCode = driverCode;
}
