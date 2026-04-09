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
) : IRequest<RegisterResultDto>;

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
    IEmailService emailService,
    ICacheService cacheService,
    IAuditService auditService,
    ITokenService tokenService,
    IUnitOfWork unitOfWork) : IRequestHandler<RegisterCommand, RegisterResultDto>
{
    private static readonly TimeSpan VerificationTtl = TimeSpan.FromMinutes(10);

    public async Task<RegisterResultDto> Handle(RegisterCommand cmd, CancellationToken ct)
    {
        var existing = await userRepository.GetByEmailWithSecurityAsync(cmd.Email, ct);
        if (existing is not null)
        {
            if (existing.EmailVerified)
                throw new ConflictException("Email is already registered.");

            var age = DateTime.UtcNow - existing.CreatedAt;
            if (age < VerificationTtl)
            {
                var remaining = VerificationTtl - age;
                throw new ConflictException($"Email chưa xác thực. Vui lòng xác thực hoặc chờ {Math.Ceiling(remaining.TotalMinutes)} phút để đăng ký lại.");
            }

            // Reclaim expired unverified account so the same email can register again.
            existing.SoftDelete();
            await unitOfWork.SaveChangesAsync(ct);
        }

        if (cmd.Phone != null && await userRepository.ExistsByPhoneAsync(cmd.Phone, ct))
            throw new ConflictException("Phone number is already registered.");

        var passwordHash = passwordHasher.Hash(cmd.Password);
        var user = User.Create(cmd.Email, passwordHash, cmd.FirstName, cmd.LastName, cmd.Phone);

        // Assign default RIDER role
        var riderRole = await roleRepository.GetByNameAsync("RIDER", ct)
            ?? throw new DomainException("SETUP_ERROR", "RIDER role is not seeded. Run migrations first.");

        user.UserRoles.Add(UserRole.Create(user.Id, riderRole.Id));

        await userRepository.AddAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);

        // Store email verification token in Redis (10-minute TTL — short-lived, no DB column needed)
        var rawToken = tokenService.GenerateSecureToken();
        await cacheService.SetAsync(
            $"email_verify:{user.Id}",
            tokenService.HashToken(rawToken),
            VerificationTtl,
            ct);

        // Send verification email (non-blocking — failures are logged, not thrown)
        _ = emailService.SendVerificationEmailAsync(user.Email, rawToken, user.Id, CancellationToken.None);

        user.ClearDomainEvents();

        await auditService.LogAsync("REGISTER", user.Id, cmd.IpAddress, cmd.UserAgent,
            new { email = user.Email }, ct);

        return new RegisterResultDto(
            user.Id,
            user.Email,
            DateTime.UtcNow.Add(VerificationTtl),
            "Đăng ký thành công. Vui lòng kiểm tra email để xác thực tài khoản trước khi đăng nhập.");
    }
}
