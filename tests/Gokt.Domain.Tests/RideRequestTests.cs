using Gokt.Domain.Entities;
using Gokt.Domain.Enums;
using Xunit;

namespace Gokt.Domain.Tests;

public class RideRequestTests
{
    [Fact]
    public void Create_ShouldSetCoreFieldsAndPendingStatus()
    {
        var customerId = Guid.NewGuid();

        var ride = RideRequest.Create(
            customerId,
            10.123, 106.123, "A",
            10.456, 106.456, "B",
            VehicleType.Seat4,
            50000m,
            4.2m);

        Assert.Equal(customerId, ride.CustomerId);
        Assert.Equal(RideStatus.Pending, ride.Status);
        Assert.Equal(VehicleType.Seat4, ride.RequestedVehicleType);
        Assert.Equal(50000m, ride.EstimatedFare);
        Assert.Equal(4.2m, ride.EstimatedDistanceKm);
        Assert.True(ride.ExpiresAt > ride.CreatedAt);
    }

    [Fact]
    public void StateTransitions_ShouldUpdateRideStatus()
    {
        var ride = RideRequest.Create(
            Guid.NewGuid(),
            10, 106, "A",
            11, 107, "B",
            VehicleType.Seat7,
            100000m);

        ride.StartSearching();
        Assert.Equal(RideStatus.Searching, ride.Status);

        ride.Accept();
        Assert.Equal(RideStatus.Accepted, ride.Status);

        ride.Cancel();
        Assert.Equal(RideStatus.Cancelled, ride.Status);

        ride.Expire();
        Assert.Equal(RideStatus.Expired, ride.Status);
    }
}
