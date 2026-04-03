using Gokt.Application.Commands.Users.UpdateProfile;
using Gokt.Application.DTOs;
using Gokt.Application.Queries.GetCurrentUser;
using Gokt.Application.Queries.GetUserSessions;
using Gokt.Application.Commands.Auth.Logout;
using Gokt.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gokt.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
[Produces("application/json")]
public class UsersController(IMediator mediator) : ControllerBase
{
    // GET /api/v1/users/me
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), 200)]
    public async Task<IActionResult> GetMe(CancellationToken ct) =>
        Ok(await mediator.Send(new GetCurrentUserQuery(CurrentUserId), ct));

    // PUT /api/v1/users/me/profile
    [HttpPut("me/profile")]
    [ProducesResponseType(typeof(UserDto), 200)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req, CancellationToken ct) =>
        Ok(await mediator.Send(new UpdateProfileCommand(
            CurrentUserId,
            req.FirstName, req.LastName, req.AvatarUrl,
            req.DateOfBirth, req.Gender, req.Address), ct));

    // GET /api/v1/users/me/sessions
    [HttpGet("me/sessions")]
    [ProducesResponseType(typeof(IEnumerable<SessionDto>), 200)]
    public async Task<IActionResult> GetSessions(CancellationToken ct) =>
        Ok(await mediator.Send(new GetUserSessionsQuery(CurrentUserId), ct));

    // DELETE /api/v1/users/me/sessions/{sessionId}
    [HttpDelete("me/sessions/{sessionId:guid}")]
    public IActionResult RevokeSession(Guid sessionId)
    {
        // TODO: implement RevokeSessionCommand
        return NoContent();
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedException("User ID claim not found."));
}

public record UpdateProfileRequest(
    string? FirstName,
    string? LastName,
    string? AvatarUrl,
    DateOnly? DateOfBirth,
    string? Gender,
    string? Address
);
