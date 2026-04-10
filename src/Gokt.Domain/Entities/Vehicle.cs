using Gokt.Domain.Enums;

namespace Gokt.Domain.Entities;

public class Vehicle
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid DriverId { get; private set; }
    public string Make { get; private set; } = default!;
    public string Model { get; private set; } = default!;
    public int Year { get; private set; }
    public string Color { get; private set; } = default!;
    public string PlateNumber { get; private set; } = default!;
    public int SeatCount { get; private set; }
    public string? ImageUrl { get; private set; }
    public VehicleType VehicleType { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsVerified { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public Driver Driver { get; private set; } = default!;

    private Vehicle() { }

    public static Vehicle Create(
        Guid driverId, string make, string model, int year,
        string color, string plateNumber, int seatCount, VehicleType vehicleType, string? imageUrl = null) =>
        new()
        {
            DriverId = driverId,
            Make = make,
            Model = model,
            Year = year,
            Color = color,
            PlateNumber = plateNumber.ToUpperInvariant(),
            SeatCount = seatCount,
            ImageUrl = imageUrl,
            VehicleType = vehicleType
        };

    public void Verify()
    {
        IsVerified = true;
        VerifiedAt = DateTime.UtcNow;
    }

    public void UpdateVisualInfo(string color, string? imageUrl, int seatCount)
    {
        Color = color;
        ImageUrl = imageUrl;
        SeatCount = seatCount;
    }

    public void RequestUpdate(
        string make,
        string model,
        int year,
        string color,
        string plateNumber,
        int seatCount,
        VehicleType vehicleType,
        string? imageUrl)
    {
        Make = make;
        Model = model;
        Year = year;
        Color = color;
        PlateNumber = plateNumber.ToUpperInvariant();
        SeatCount = seatCount;
        VehicleType = vehicleType;
        ImageUrl = imageUrl;

        // Any changed vehicle details require admin re-verification.
        IsVerified = false;
        VerifiedAt = null;
    }

    public void Deactivate() => IsActive = false;
}
