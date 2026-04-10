using Gokt.Application.Commands.Trips.CompleteTrip;
using Gokt.Application.Commands.Trips.RateTrip;
using Gokt.Application.Commands.Trips.SendTripMessage;
using Gokt.Application.Commands.Trips.UpdateTripStatus;
using Gokt.Application.DTOs;
using Gokt.Application.Queries.GetTripHistory;
using Gokt.Application.Queries.GetTripMessages;
using Gokt.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gokt.Controllers;

[ApiController]
[Route("api/v1/trips")]
[Produces("application/json")]
[Authorize]
public class TripsController(IMediator mediator) : ControllerBase
{
    // PUT /api/v1/trips/{id}/status
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(TripDto), 200)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateTripStatusCommand(CurrentUserId, id, req.Status), ct);
        return Ok(result);
    }

    // POST /api/v1/trips/{id}/complete
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(TripDto), 200)]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteTripRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new CompleteTripCommand(CurrentUserId, id, req.ActualDistanceKm), ct);
        return Ok(result);
    }

    // POST /api/v1/trips/{id}/rate
    [HttpPost("{id:guid}/rate")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Rate(Guid id, [FromBody] RateTripRequest req, CancellationToken ct)
    {
        await mediator.Send(new RateTripCommand(CurrentUserId, id, req.Rating, req.Comment), ct);
        return NoContent();
    }

    // GET /api/v1/trips/history?page=1&pageSize=20
    [HttpGet("history")]
    [ProducesResponseType(typeof(IEnumerable<TripDto>), 200)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetTripHistoryQuery(CurrentUserId, page, pageSize), ct);
        return Ok(result);
    }

    // GET /api/v1/trips/{id}/messages
    [HttpGet("{id:guid}/messages")]
    [ProducesResponseType(typeof(IEnumerable<TripMessageDto>), 200)]
    public async Task<IActionResult> GetMessages(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetTripMessagesQuery(CurrentUserId, id), ct);
        return Ok(result);
    }

    // POST /api/v1/trips/{id}/messages
    [HttpPost("{id:guid}/messages")]
    [ProducesResponseType(typeof(TripMessageDto), 200)]
    public async Task<IActionResult> SendMessage(Guid id, [FromBody] SendMessageRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new SendTripMessageCommand(CurrentUserId, id, req.Body), ct);
        return Ok(result);
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID claim not found."));
}

public record UpdateStatusRequest(TripStatus Status);
public record CompleteTripRequest(decimal ActualDistanceKm);
public record RateTripRequest(int Rating, string? Comment);
public record SendMessageRequest(string Body);
