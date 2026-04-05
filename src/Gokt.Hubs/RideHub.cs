using Gokt.Application.Commands.Drivers.UpdateDriverLocation;
using Gokt.Application.Commands.Rides.AcceptRideRequest;
using Gokt.Application.Commands.Rides.DeclineRide;
using Gokt.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Gokt.Hubs;

[Authorize]
public class RideHub(
    IMediator mediator,
    IDriverRepository driverRepository,
    ILocationService locationService) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            Context.Abort();
            return;
        }

        // Always join the customer group (all users are also customers)
        await Groups.AddToGroupAsync(Context.ConnectionId, $"customer:{userId}");

        // If this user is a driver, also join the driver group + register connection
        var driver = await driverRepository.GetByUserIdAsync(userId);
        if (driver is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"driver:{driver.Id}");
            await locationService.AddDriverConnectionAsync(driver.Id, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId != Guid.Empty)
        {
            var driver = await driverRepository.GetByUserIdAsync(userId);
            if (driver is not null)
                await locationService.RemoveDriverConnectionAsync(driver.Id, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Driver sends current GPS coordinates; broadcasts to customer on active trip.
    /// </summary>
    public async Task SendLocation(double latitude, double longitude)
    {
        var userId = GetUserId();
        if (!IsDriver()) return;

        await mediator.Send(new UpdateDriverLocationCommand(userId, latitude, longitude));
    }

    /// <summary>
    /// Driver accepts a ride offer received via ReceiveRideOffer.
    /// Returns TripDto to the calling driver via "RideAccepted" event.
    /// </summary>
    public async Task AcceptRide(Guid rideRequestId)
    {
        var userId = GetUserId();
        if (!IsDriver())
        {
            await Clients.Caller.SendAsync("Error", "Only drivers can accept rides.");
            return;
        }

        try
        {
            var trip = await mediator.Send(new AcceptRideRequestCommand(userId, rideRequestId));
            await Clients.Caller.SendAsync("RideAccepted", trip);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Driver declines a ride offer. Removes them from the candidate list.
    /// </summary>
    public async Task DeclineRide(Guid rideRequestId)
    {
        var userId = GetUserId();
        if (!IsDriver()) return;

        await mediator.Send(new DeclineRideCommand(userId, rideRequestId));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Guid GetUserId()
    {
        var sub = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? Context.User?.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    private bool IsDriver() =>
        Context.User?.IsInRole("DRIVER") ?? false;
}
