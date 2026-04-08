using Gokt.Application.Commands.Auth.ChangePassword;
using Gokt.Application.Commands.Auth.ForgotPassword;
using Gokt.Application.Commands.Auth.GoogleOAuth;
using Gokt.Application.Commands.Auth.Login;
using Gokt.Application.Commands.Auth.Logout;
using Gokt.Application.Commands.Auth.RefreshToken;
using Gokt.Application.Commands.Auth.Register;
using Gokt.Application.Commands.Auth.ResetPassword;
using Gokt.Application.Commands.Auth.VerifyEmail;
using Gokt.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Gokt.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController(IMediator mediator) : ControllerBase
{
    // POST /api/v1/auth/register
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthTokensDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new RegisterCommand(
            req.Email, req.Password, req.FirstName, req.LastName, req.Phone,
            GetClientIp(), Request.Headers.UserAgent.ToString()), ct);

        return StatusCode(201, result);
    }

    // POST /api/v1/auth/login
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthTokensDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(429)]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new LoginCommand(
            req.Email, req.Password,
            GetClientIp(), Request.Headers.UserAgent.ToString()), ct);

        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiry);
        return Ok(result);
    }

    // POST /api/v1/auth/refresh
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthTokensDto), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest? req, CancellationToken ct)
    {
        var token = Request.Cookies["refresh_token"] ?? req?.RefreshToken;
        if (string.IsNullOrEmpty(token))
            return Unauthorized(new { code = "MISSING_TOKEN", message = "Refresh token is required." });

        var result = await mediator.Send(new RefreshTokenCommand(token, GetClientIp()), ct);
        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiry);
        return Ok(result);
    }

    // POST /api/v1/auth/logout
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? req, CancellationToken ct)
    {
        var token = Request.Cookies["refresh_token"] ?? req?.RefreshToken;
        var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        await mediator.Send(new LogoutCommand(CurrentUserId, token, LogoutAll: false, Jti: jti), ct);
        Response.Cookies.Delete("refresh_token");
        return NoContent();
    }

    // POST /api/v1/auth/logout-all
    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutAll(CancellationToken ct)
    {
        var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        await mediator.Send(new LogoutCommand(CurrentUserId, null, LogoutAll: true, Jti: jti), ct);
        Response.Cookies.Delete("refresh_token");
        return NoContent();
    }

    // POST /api/v1/auth/verify-email
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest req, CancellationToken ct)
    {
        await mediator.Send(new VerifyEmailCommand(req.UserId, req.Token), ct);
        return Ok(new { message = "Email verified successfully." });
    }

    // GET /api/v1/auth/verify-email?userId=...&token=... (email link click)
    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmailGet([FromQuery] Guid userId, [FromQuery] string token, CancellationToken ct)
    {
        await mediator.Send(new VerifyEmailCommand(userId, token), ct);
        return Ok(new { message = "Email verified successfully." });
    }

    // POST /api/v1/auth/forgot-password
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req, CancellationToken ct)
    {
        await mediator.Send(new ForgotPasswordCommand(req.Email), ct);
        return Ok(new { message = "If that email exists, a password reset link has been sent." });
    }

    // POST /api/v1/auth/reset-password
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req, CancellationToken ct)
    {
        await mediator.Send(new ResetPasswordCommand(req.Token, req.NewPassword), ct);
        return Ok(new { message = "Password reset successfully. Please log in again." });
    }

    // POST /api/v1/auth/change-password
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req, CancellationToken ct)
    {
        await mediator.Send(new ChangePasswordCommand(CurrentUserId, req.CurrentPassword, req.NewPassword), ct);
        return Ok(new { message = "Password changed. All sessions have been revoked." });
    }

    // POST /api/v1/auth/google
    [HttpPost("google")]
    [ProducesResponseType(typeof(AuthTokensDto), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Google([FromBody] GoogleRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new GoogleOAuthCommand(
            req.IdToken, GetClientIp(), Request.Headers.UserAgent.ToString()), ct);
        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiry);
        return Ok(result);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID claim not found."));

    private string? GetClientIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();

    private void SetRefreshTokenCookie(string token, DateTime expiry)
    {
        Response.Cookies.Append("refresh_token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expiry,
            Path = "/api/v1/auth"
        });
    }
}

// ── Request models ───────────────────────────────────────────────────────────

public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? Phone
);

public record LoginRequest(
    string Email,
    string Password
);

public record RefreshRequest(string RefreshToken);

public record LogoutRequest(string? RefreshToken);

public record VerifyEmailRequest(Guid UserId, string Token);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Token, string NewPassword);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record GoogleRequest(string IdToken);
