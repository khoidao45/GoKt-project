using Gokt.Application.Commands.Notifications.MarkNotificationsRead;
using Gokt.Application.DTOs;
using Gokt.Application.Queries.GetNotifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gokt.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Produces("application/json")]
[Authorize]
public class NotificationsController(IMediator mediator) : ControllerBase
{
    // GET /api/v1/notifications?page=1&pageSize=20
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<NotificationDto>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetNotificationsQuery(CurrentUserId, page, pageSize), ct);
        return Ok(result);
    }

    // PUT /api/v1/notifications/read
    [HttpPut("read")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> MarkRead([FromBody] MarkReadRequest req, CancellationToken ct)
    {
        await mediator.Send(new MarkNotificationsReadCommand(CurrentUserId, req.NotificationIds), ct);
        return NoContent();
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID claim not found."));
}

public record MarkReadRequest(List<Guid> NotificationIds);
