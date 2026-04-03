using Gokt.Application.Interfaces;
using Gokt.Domain.Exceptions;
using MediatR;

namespace Gokt.Application.Commands.Auth.Logout;

public record LogoutCommand(
    Guid UserId,
    string? RefreshToken,
    bool LogoutAll,
    string? Jti = null   // JTI from the current access token — blacklisted in Redis on logout
) : IRequest;

public sealed class LogoutCommandHandler(
    ISessionRepository sessionRepository,
    ITokenService tokenService,
    ICacheService cacheService,
    IAuditService auditService,
    IUnitOfWork unitOfWork) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand cmd, CancellationToken ct)
    {
        if (cmd.LogoutAll)
        {
            await sessionRepository.RevokeAllByUserIdAsync(cmd.UserId, ct);
        }
        else if (!string.IsNullOrEmpty(cmd.RefreshToken))
        {
            var tokenHash = tokenService.HashToken(cmd.RefreshToken);
            var session = await sessionRepository.GetByTokenHashAsync(tokenHash, ct)
                ?? throw new UnauthorizedException("Invalid session.");

            if (session.UserId != cmd.UserId)
                throw new ForbiddenException("Cannot revoke another user's session.");

            session.Revoke();
        }

        await unitOfWork.SaveChangesAsync(ct);
        await cacheService.RemoveAsync($"user:{cmd.UserId}", ct);

        // Blacklist the current access token by its JTI so it's rejected even before expiry
        // TTL matches the maximum remaining lifetime of a 15-minute access token
        if (!string.IsNullOrEmpty(cmd.Jti))
            await cacheService.SetAsync($"blacklist:{cmd.Jti}", true, TimeSpan.FromMinutes(15), ct);

        await auditService.LogAsync("LOGOUT", cmd.UserId, null, null,
            new { logoutAll = cmd.LogoutAll }, ct);
    }
}
