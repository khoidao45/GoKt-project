using Gokt.Application.Commands.Rides.AcceptRideRequest;
using Gokt.Application.Commands.Rides.CancelRideRequest;
using Gokt.Application.Commands.Rides.CreateRideRequest;
using Gokt.Application.DTOs;
using Gokt.Application.Queries.GetActiveRide;
using Gokt.Application.Queries.GetPriceEstimate;
using Gokt.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gokt.Controllers;

[ApiController]
[Route("api/v1/rides")]
[Produces("application/json")]
[Authorize]
public class RidesController(IMediator mediator) : ControllerBase
{
    // GET /api/v1/rides/estimate?pickupLat=...&pickupLng=...&dropoffLat=...&dropoffLng=...&vehicleType=Economy
    [HttpGet("estimate")]
    [ProducesResponseType(typeof(PriceEstimateDto), 200)]
    public async Task<IActionResult> GetEstimate(
        [FromQuery] double pickupLat, [FromQuery] double pickupLng,
        [FromQuery] double dropoffLat, [FromQuery] double dropoffLng,
        [FromQuery] VehicleType vehicleType = VehicleType.Economy,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new GetPriceEstimateQuery(pickupLat, pickupLng, dropoffLat, dropoffLng, vehicleType), ct);
        return Ok(result);
    }

    // POST /api/v1/rides/request
    [HttpPost("request")]
    [ProducesResponseType(typeof(RideRequestDto), 201)]
    public async Task<IActionResult> CreateRequest([FromBody] CreateRideRequestBody req, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateRideRequestCommand(
            CurrentUserId,
            req.PickupLatitude, req.PickupLongitude, req.PickupAddress,
            req.DropoffLatitude, req.DropoffLongitude, req.DropoffAddress,
            req.VehicleType), ct);
        return StatusCode(201, result);
    }

    // POST /api/v1/rides/{id}/accept
    [HttpPost("{id:guid}/accept")]
    [ProducesResponseType(typeof(TripDto), 200)]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new AcceptRideRequestCommand(CurrentUserId, id), ct);
        return Ok(result);
    }

    // POST /api/v1/rides/{id}/cancel
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelRideBody? req, CancellationToken ct)
    {
        await mediator.Send(new CancelRideRequestCommand(CurrentUserId, id, req?.Reason), ct);
        return NoContent();
    }

    // GET /api/v1/rides/active
    [HttpGet("active")]
    [ProducesResponseType(typeof(ActiveRideDto), 200)]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var result = await mediator.Send(new GetActiveRideQuery(CurrentUserId), ct);
        return Ok(result);
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID claim not found."));
}

public record CreateRideRequestBody(
    double PickupLatitude, double PickupLongitude, string PickupAddress,
    double DropoffLatitude, double DropoffLongitude, string DropoffAddress,
    VehicleType VehicleType
);
public record CancelRideBody(string? Reason);
