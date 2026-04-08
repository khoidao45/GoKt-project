using FluentValidation;
using Gokt.Application.DTOs;
using Gokt.Application.Interfaces;
using Gokt.Domain.Entities;
using Gokt.Domain.Exceptions;
using MediatR;

namespace Gokt.Application.Commands.Auth.Register;

public record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? Phone,
    string? IpAddress,
    string? UserAgent
) : IRequest<AuthTokensDto>;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(255);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(128)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).MaximumLength(20).When(x => x.Phone != null);
    }
}

public sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IEmailService emailService,
    ICacheService cacheService,
    IAuditService auditService,
    IUnitOfWork unitOfWork) : IRequestHandler<RegisterCommand, AuthTokensDto>
{
    public async Task<AuthTokensDto> Handle(RegisterCommand cmd, CancellationToken ct)
    {
        if (await userRepository.ExistsByEmailAsync(cmd.Email, ct))
            throw new ConflictException("Email is already registered.");

        if (cmd.Phone != null && await userRepository.ExistsByPhoneAsync(cmd.Phone, ct))
            throw new ConflictException("Phone number is already registered.");

        var passwordHash = passwordHasher.Hash(cmd.Password);
        var user = User.Create(cmd.Email, passwordHash, cmd.FirstName, cmd.LastName, cmd.Phone);

        // Assign default RIDER role
        var riderRole = await roleRepository.GetByNameAsync("RIDER", ct)
            ?? throw new DomainException("SETUP_ERROR", "RIDER role is not seeded. Run migrations first.");

        user.UserRoles.Add(UserRole.Create(user.Id, riderRole.Id));

        // Issue tokens BEFORE saving so we can create session in one SaveChanges call
        var (accessToken, accessExpiry) = tokenService.GenerateAccessToken(user);
        var (rawRefresh, refreshExpiry) = tokenService.GenerateRefreshToken();

        var session = UserSession.Create(
            user.Id, tokenService.HashToken(rawRefresh), refreshExpiry,
            cmd.IpAddress, cmd.UserAgent);

        user.Sessions.Add(session);

        await userRepository.AddAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);

        // Store email verification token in Redis (10-minute TTL — short-lived, no DB column needed)
        var rawToken = tokenService.GenerateSecureToken();
        await cacheService.SetAsync(
            $"email_verify:{user.Id}",
            tokenService.HashToken(rawToken),
            TimeSpan.FromMinutes(10),
            ct);

        // Send verification email (non-blocking — failures are logged, not thrown)
        _ = emailService.SendVerificationEmailAsync(user.Email, rawToken, user.Id, CancellationToken.None);

        user.ClearDomainEvents();

        await auditService.LogAsync("REGISTER", user.Id, cmd.IpAddress, cmd.UserAgent,
            new { email = user.Email }, ct);

        return new AuthTokensDto(accessToken, accessExpiry, rawRefresh, refreshExpiry, UserDto.From(user));
    }
}
