using Gokt.Application.DTOs;
using Gokt.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Gokt.Hubs;

public class SignalRRealtimeService(IHubContext<RideHub> hubContext) : IRealtimeService
{
    public async Task NotifyDriverOfRideAsync(
        IReadOnlyList<string> connectionIds, RideOfferPayload payload, CancellationToken ct = default)
    {
        foreach (var connId in connectionIds)
            await hubContext.Clients.Client(connId).SendAsync("ReceiveRideOffer", payload, ct);
    }

    public async Task NotifyDriverRideTakenAsync(
        IReadOnlyList<string> connectionIds, Guid rideRequestId, CancellationToken ct = default)
    {
        foreach (var connId in connectionIds)
            await hubContext.Clients.Client(connId).SendAsync("RideTaken", rideRequestId, ct);
    }

    public Task NotifyCustomerDriverFoundAsync(
        Guid customerId, DriverFoundPayload payload, CancellationToken ct = default)
        => hubContext.Clients.Group($"customer:{customerId}").SendAsync("DriverFound", payload, ct);

    public Task NotifyCustomerNoDriverFoundAsync(
        Guid customerId, Guid rideRequestId, CancellationToken ct = default)
        => hubContext.Clients.Group($"customer:{customerId}").SendAsync("NoDriverFound", rideRequestId, ct);

    public Task BroadcastDriverLocationAsync(
        Guid customerId, DriverLocationPayload payload, CancellationToken ct = default)
        => hubContext.Clients.Group($"customer:{customerId}").SendAsync("DriverLocationUpdate", payload, ct);

    public Task NotifyRideCancelledAsync(
        Guid targetUserId, Guid rideRequestId, string reason, CancellationToken ct = default)
        => hubContext.Clients.Group($"customer:{targetUserId}")
            .SendAsync("RideCancelled", new { rideRequestId, reason }, ct);

    public Task SendTripMessageAsync(
        Guid driverId, Guid customerId, TripMessageDto message, CancellationToken ct = default)
        => Task.WhenAll(
            hubContext.Clients.Group($"driver:{driverId}").SendAsync("ReceiveMessage", message, ct),
            hubContext.Clients.Group($"customer:{customerId}").SendAsync("ReceiveMessage", message, ct));
}
