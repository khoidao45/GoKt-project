using Gokt.Application.Commands.Drivers.AddVehicle;
using Gokt.Application.Commands.Drivers.RegisterDriver;
using Gokt.Application.Commands.Drivers.ToggleDriverOnline;
using Gokt.Application.Commands.Drivers.UpdateDriverLocation;
using Gokt.Application.Commands.Trips.RateTrip;
using Gokt.Application.DTOs;
using Gokt.Application.Queries.GetDriverTripHistory;
using Gokt.Application.Queries.GetMyDriverProfile;
using Gokt.Application.Queries.GetNearbyDrivers;
using Gokt.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gokt.Controllers;

[ApiController]
[Route("api/v1/drivers")]
[Produces("application/json")]
[Authorize]
public class DriversController(IMediator mediator) : ControllerBase
{
    // POST /api/v1/drivers/register
    [HttpPost("register")]
    [ProducesResponseType(typeof(DriverDto), 201)]
    public async Task<IActionResult> Register([FromBody] RegisterDriverRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new RegisterDriverCommand(CurrentUserId, req.LicenseNumber, DateTime.SpecifyKind(req.LicenseExpiry, DateTimeKind.Utc)), ct);
        return StatusCode(201, result);
    }

    // POST /api/v1/drivers/vehicles
    // Supports categories: ElectricBike, Seat4, Seat7, Seat9.
    [HttpPost("vehicles")]
    [ProducesResponseType(typeof(VehicleDto), 201)]
    public async Task<IActionResult> AddVehicle([FromBody] AddVehicleRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new AddVehicleCommand(
            CurrentUserId, req.Make, req.Model, req.Year, req.Color, req.PlateNumber, req.SeatCount, req.ImageUrl, req.VehicleType), ct);
        return StatusCode(201, result);
    }

    // PUT /api/v1/drivers/online
    [HttpPut("online")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> ToggleOnline([FromBody] ToggleOnlineRequest req, CancellationToken ct)
    {
        await mediator.Send(new ToggleDriverOnlineCommand(CurrentUserId, req.IsOnline), ct);
        return NoContent();
    }

    // PUT /api/v1/drivers/location
    [HttpPut("location")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationRequest req, CancellationToken ct)
    {
        await mediator.Send(new UpdateDriverLocationCommand(CurrentUserId, req.Latitude, req.Longitude), ct);
        return NoContent();
    }

    // GET /api/v1/drivers/me
    [HttpGet("me")]
    [ProducesResponseType(typeof(DriverDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var driver = await mediator.Send(new GetMyDriverProfileQuery(CurrentUserId), ct);
        if (driver is null) return NotFound();
        return Ok(driver);
    }

    // GET /api/v1/drivers/nearby?lat=...&lng=...&radius=5&vehicleType=Seat4
    [HttpGet("nearby")]
    [ProducesResponseType(typeof(IEnumerable<DriverDto>), 200)]
    public async Task<IActionResult> GetNearby(
        [FromQuery] double lat, [FromQuery] double lng,
        [FromQuery] double radius = 5,
        [FromQuery] VehicleType? vehicleType = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetNearbyDriversQuery(lat, lng, radius, vehicleType), ct);
        return Ok(result);
    }

    // GET /api/v1/drivers/trips?page=1&pageSize=20
    [HttpGet("trips")]
    [ProducesResponseType(typeof(IEnumerable<TripDto>), 200)]
    public async Task<IActionResult> GetTripHistory(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetDriverTripHistoryQuery(CurrentUserId, page, pageSize), ct);
        return Ok(result);
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID claim not found."));
}

public record RegisterDriverRequest(string LicenseNumber, DateTime LicenseExpiry);
public record AddVehicleRequest(string Make, string Model, int Year, string Color, string PlateNumber, int SeatCount, string? ImageUrl, VehicleType VehicleType);
public record ToggleOnlineRequest(bool IsOnline);
public record UpdateLocationRequest(double Latitude, double Longitude);
