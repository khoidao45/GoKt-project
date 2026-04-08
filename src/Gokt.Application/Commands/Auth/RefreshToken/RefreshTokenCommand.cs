using Gokt.Application.DTOs;
using Gokt.Application.Interfaces;
using Gokt.Domain.Exceptions;
using MediatR;

namespace Gokt.Application.Commands.Auth.RefreshToken;

public record RefreshTokenCommand(string RefreshToken, string? IpAddress) : IRequest<AuthTokensDto>;

public sealed class RefreshTokenCommandHandler(
    ISessionRepository sessionRepository,
    IUserRepository userRepository,
    ITokenService tokenService,
    IUnitOfWork unitOfWork) : IRequestHandler<RefreshTokenCommand, AuthTokensDto>
{
    public async Task<AuthTokensDto> Handle(RefreshTokenCommand cmd, CancellationToken ct)
    {
        var tokenHash = tokenService.HashToken(cmd.RefreshToken);
        var session = await sessionRepository.GetByTokenHashAsync(tokenHash, ct)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        // Theft detection: a revoked token being reused means the raw token was stolen.
        // Nuke ALL sessions for this user immediately.
        if (session.IsRevoked)
        {
            await sessionRepository.RevokeAllByUserIdAsync(session.UserId, ct);
            await unitOfWork.SaveChangesAsync(ct);
            throw new UnauthorizedException("Security breach detected. All sessions have been revoked. Please log in again.");
        }

        if (session.ExpiresAt <= DateTime.UtcNow)
            throw new UnauthorizedException("Session has expired. Please log in again.");

        var user = await userRepository.GetByIdWithRolesAsync(session.UserId, ct)
            ?? throw new UnauthorizedException("User not found.");

        if (user.Status == Domain.Enums.UserStatus.Suspended)
            throw new ForbiddenException("Your account has been suspended.");

        if (user.Status == Domain.Enums.UserStatus.Deleted)
            throw new UnauthorizedException("User not found.");

        // Rotate: old session gets ReplacedByTokenHash set; new hash replaces it
        var (newRawRefresh, newRefreshExpiry) = tokenService.GenerateRefreshToken();
        session.RotateToken(tokenService.HashToken(newRawRefresh), newRefreshExpiry);

        var (newAccessToken, newAccessExpiry) = tokenService.GenerateAccessToken(user);

        await unitOfWork.SaveChangesAsync(ct);

        return new AuthTokensDto(newAccessToken, newAccessExpiry, newRawRefresh, newRefreshExpiry, UserDto.From(user));
    }
}
