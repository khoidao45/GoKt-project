using Gokt.Domain.Enums;

namespace Gokt.Domain.Entities;

public class Trip
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid RideRequestId { get; private set; }
    public Guid DriverId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid VehicleId { get; private set; }
    public TripStatus Status { get; private set; } = TripStatus.Accepted;
    public DateTime AcceptedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? DriverArrivedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public decimal? FinalFare { get; private set; }
    public decimal? ActualDistanceKm { get; private set; }
    public int? ActualDurationMinutes { get; private set; }
    public int? CustomerRating { get; private set; }
    public int? DriverRating { get; private set; }
    public string? CustomerRatingComment { get; private set; }
    public string? DriverRatingComment { get; private set; }

    public RideRequest RideRequest { get; private set; } = default!;
    public Driver Driver { get; private set; } = default!;
    public Vehicle Vehicle { get; private set; } = default!;
    public User Customer { get; private set; } = default!;

    private Trip() { }

    public static Trip Create(Guid rideRequestId, Guid driverId, Guid customerId, Guid vehicleId) =>
        new()
        {
            RideRequestId = rideRequestId,
            DriverId = driverId,
            CustomerId = customerId,
            VehicleId = vehicleId
        };

    public void SetDriverEnRoute() => Status = TripStatus.DriverEnRoute;

    public void SetDriverArrived()
    {
        Status = TripStatus.DriverArrived;
        DriverArrivedAt = DateTime.UtcNow;
    }

    public void Start()
    {
        Status = TripStatus.InProgress;
        StartedAt = DateTime.UtcNow;
    }

    public void Complete(decimal finalFare, decimal actualDistanceKm)
    {
        Status = TripStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        FinalFare = finalFare;
        ActualDistanceKm = actualDistanceKm;
        ActualDurationMinutes = StartedAt.HasValue
            ? (int)(DateTime.UtcNow - StartedAt.Value).TotalMinutes
            : null;
    }

    public void Cancel(string? reason = null)
    {
        Status = TripStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
    }

    public void RateByCustomer(int rating, string? comment = null)
    {
        CustomerRating = Math.Clamp(rating, 1, 5);
        CustomerRatingComment = comment;
    }

    public void RateByDriver(int rating, string? comment = null)
    {
        DriverRating = Math.Clamp(rating, 1, 5);
        DriverRatingComment = comment;
    }
}
