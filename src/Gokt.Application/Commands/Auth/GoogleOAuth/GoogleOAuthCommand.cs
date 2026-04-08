using Gokt.Application.DTOs;
using Gokt.Application.Interfaces;
using Gokt.Domain.Entities;
using Gokt.Domain.Enums;
using Gokt.Domain.Exceptions;
using Google.Apis.Auth;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Gokt.Application.Commands.Auth.GoogleOAuth;

public record GoogleOAuthCommand(
    string IdToken,
    string? IpAddress,
    string? UserAgent
) : IRequest<AuthTokensDto>;

public sealed class GoogleOAuthCommandHandler(
    IOAuthRepository oauthRepository,
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    ISessionRepository sessionRepository,
    ITokenService tokenService,
    ICacheService cacheService,
    IUnitOfWork unitOfWork,
    IConfiguration configuration) : IRequestHandler<GoogleOAuthCommand, AuthTokensDto>
{
    public async Task<AuthTokensDto> Handle(GoogleOAuthCommand cmd, CancellationToken ct)
    {
        var clientId = configuration["Google:ClientId"]
            ?? throw new InvalidOperationException("Google:ClientId is not configured.");

        // Verify Google ID token
        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(cmd.IdToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [clientId]
                });
        }
        catch (InvalidJwtException)
        {
            throw new UnauthorizedException("Invalid Google token.");
        }

        var providerUserId = payload.Subject;
        var email = payload.Email?.ToLowerInvariant()
            ?? throw new UnauthorizedException("Google account has no email.");
        var firstName = payload.GivenName;
        var lastName = payload.FamilyName;
        var avatarUrl = payload.Picture;

        // 1. Check existing OAuth link
        var existing = await oauthRepository.GetByProviderAsync("GOOGLE", providerUserId, ct);
        User user;

        if (existing != null)
        {
            // Already linked — just log in
            user = existing.User;
        }
        else
        {
            // 2. Check if email already registered → link OAuth to that account
            var byEmail = await userRepository.GetByEmailAsync(email, ct);
            if (byEmail != null)
            {
                user = byEmail;
                var link = OAuthAccount.Create(user.Id, "GOOGLE", providerUserId, email);
                await oauthRepository.AddAsync(link, ct);
            }
            else
            {
                // 3. New user — create account (no password, email already verified by Google)
                var riderRole = await roleRepository.GetByNameAsync("RIDER", ct)
                    ?? throw new DomainException("SETUP_ERROR", "RIDER role is not seeded.");

                user = User.CreateOAuth(email, firstName, lastName, avatarUrl);
                user.UserRoles.Add(UserRole.Create(user.Id, riderRole.Id));

                var oauthAccount = OAuthAccount.Create(user.Id, "GOOGLE", providerUserId, email);
                user.OAuthAccounts.Add(oauthAccount);

                await userRepository.AddAsync(user, ct);
            }

            await unitOfWork.SaveChangesAsync(ct);
        }

        if (user.Status == UserStatus.Suspended)
            throw new ForbiddenException("Your account has been suspended.");

        if (user.Status == UserStatus.Deleted)
            throw new UnauthorizedException("Account not found.");

        // Issue tokens
        var (accessToken, accessExpiry) = tokenService.GenerateAccessToken(user);
        var (rawRefresh, refreshExpiry) = tokenService.GenerateRefreshToken();

        var session = UserSession.Create(
            user.Id, tokenService.HashToken(rawRefresh), refreshExpiry,
            cmd.IpAddress, cmd.UserAgent);

        await sessionRepository.AddAsync(session, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await cacheService.SetAsync($"user:{user.Id}", UserDto.From(user), TimeSpan.FromMinutes(30), ct);

        return new AuthTokensDto(accessToken, accessExpiry, rawRefresh, refreshExpiry, UserDto.From(user));
    }
}
