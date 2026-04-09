using FluentValidation;
using Gokt.Application.DTOs;
using Gokt.Application.Interfaces;
using Gokt.Domain.Entities;
using Gokt.Domain.Exceptions;
using MediatR;

namespace Gokt.Application.Commands.Auth.Login;

public record LoginCommand(
    string Email,
    string Password,
    string? IpAddress,
    string? UserAgent
) : IRequest<AuthTokensDto>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    ICacheService cacheService,
    IEmailService emailService,
    IAuditService auditService,
    IUnitOfWork unitOfWork) : IRequestHandler<LoginCommand, AuthTokensDto>
{
    public async Task<AuthTokensDto> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var rateLimitKey = $"login_fail:{cmd.Email.ToLowerInvariant()}";

        // Redis fast-path rate limit — checked BEFORE hitting the DB
        // GetAsync<int> returns 0 (default) when key is absent
        var failCount = await cacheService.GetAsync<int>(rateLimitKey, ct);
        if (failCount >= 5)
            throw new TooManyRequestsException(
                "Too many failed login attempts. Please try again in 5 minutes.");

        var user = await userRepository.GetByEmailWithSecurityAsync(cmd.Email, ct);

        // Constant-time path when user not found — prevents email enumeration
        if (user is null)
        {
            passwordHasher.Verify("dummy", "$argon2id$v=19$m=19456,t=2,p=1$dGVzdA==$dGVzdA==");
            throw new UnauthorizedException();
        }

        if (user.Status == Domain.Enums.UserStatus.Suspended)
            throw new ForbiddenException("Your account has been suspended. Please contact support.");

        if (user.Status == Domain.Enums.UserStatus.Deleted)
            throw new UnauthorizedException();

        if (!user.EmailVerified)
        {
            var rawToken = tokenService.GenerateSecureToken();
            await cacheService.SetAsync(
                $"email_verify:{user.Id}",
                tokenService.HashToken(rawToken),
                TimeSpan.FromMinutes(10),
                ct);

            _ = emailService.SendVerificationEmailAsync(user.Email, rawToken, user.Id, CancellationToken.None);
            throw new ForbiddenException("EMAIL_NOT_VERIFIED: Vui lòng xác thực email trước khi đăng nhập. Chúng tôi đã gửi lại mã xác thực.");
        }

        // DB lockout is the authoritative guard; Redis is a fast-path pre-filter
        if (user.Security.IsLockedOut())
            throw new TooManyRequestsException(
                $"Account locked due to too many failed attempts. Try again after {user.Security.LockoutUntil:u}.");

        if (!passwordHasher.Verify(cmd.Password, user.PasswordHash!))
        {
            user.Security.RecordFailedLogin();
            await unitOfWork.SaveChangesAsync(ct);

            // Non-atomic Get+Set increment — acceptable trade-off: DB lockout is authoritative
            var currentCount = await cacheService.GetAsync<int>(rateLimitKey, ct);
            await cacheService.SetAsync(rateLimitKey, currentCount + 1, TimeSpan.FromMinutes(5), ct);

            await auditService.LogAsync("LOGIN_FAILURE", user.Id, cmd.IpAddress, cmd.UserAgent,
                new { reason = "invalid_password" }, ct);

            throw new UnauthorizedException();
        }

        // 2FA gate (future: return partial result requiring TOTP)
        if (user.Security.TwoFactorEnabled)
            throw new ForbiddenException("TWO_FACTOR_REQUIRED");

        // Generate tokens
        var (accessToken, accessExpiry) = tokenService.GenerateAccessToken(user);
        var (rawRefresh, refreshExpiry) = tokenService.GenerateRefreshToken();

        var session = UserSession.Create(
            user.Id, tokenService.HashToken(rawRefresh), refreshExpiry,
            cmd.IpAddress, cmd.UserAgent);

        user.Security.RecordSuccessfulLogin(cmd.IpAddress ?? "unknown");
        await sessionRepository.AddAsync(session, ct);
        await unitOfWork.SaveChangesAsync(ct);

        // Clear the Redis fail counter on successful login
        await cacheService.RemoveAsync(rateLimitKey, ct);

        // Cache user profile for downstream services
        await cacheService.SetAsync($"user:{user.Id}", UserDto.From(user), TimeSpan.FromMinutes(30), ct);

        await auditService.LogAsync("LOGIN_SUCCESS", user.Id, cmd.IpAddress, cmd.UserAgent, ct: ct);

        return new AuthTokensDto(accessToken, accessExpiry, rawRefresh, refreshExpiry, UserDto.From(user));
    }
}
