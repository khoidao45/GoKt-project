using Gokt.Domain.Enums;

namespace Gokt.Domain.Entities;

public class Driver
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string LicenseNumber { get; private set; } = default!;
    public DateTime LicenseExpiry { get; private set; }
    public DriverStatus Status { get; private set; } = DriverStatus.Pending;
    public decimal Rating { get; private set; } = 5.0m;
    public int TotalRides { get; private set; }
    public bool IsOnline { get; private set; }
    public bool IsBusy { get; private set; }
    public string? DriverCode { get; private set; }
    public double? CurrentLatitude { get; private set; }
    public double? CurrentLongitude { get; private set; }
    public DateTime? LastLocationUpdatedAt { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public User User { get; private set; } = default!;
    public ICollection<Vehicle> Vehicles { get; private set; } = new List<Vehicle>();
    public ICollection<Trip> Trips { get; private set; } = new List<Trip>();

    private Driver() { }

    public static Driver Create(Guid userId, string licenseNumber, DateTime licenseExpiry) =>
        new()
        {
            UserId = userId,
            LicenseNumber = licenseNumber,
            LicenseExpiry = licenseExpiry,
            DriverCode = GenerateDriverCode()
        };

    private static string GenerateDriverCode() =>
        "DR-" + Guid.NewGuid().ToString("N")[..8].ToUpper();

    public void GoOnline()
    {
        IsOnline = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void GoOffline()
    {
        IsOnline = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetBusy()
    {
        IsBusy = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearBusy()
    {
        IsBusy = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLocation(double latitude, double longitude)
    {
        CurrentLatitude = latitude;
        CurrentLongitude = longitude;
        LastLocationUpdatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        Status = DriverStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Suspend()
    {
        Status = DriverStatus.Suspended;
        IsOnline = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateRating(decimal newRating)
    {
        Rating = Math.Round(Math.Clamp(newRating, 1m, 5m), 1);
        TotalRides++;
        UpdatedAt = DateTime.UtcNow;
    }
}
