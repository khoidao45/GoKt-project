using Gokt.Application.Interfaces;
using Gokt.Domain.Enums;
using Gokt.Domain.Exceptions;
using MediatR;

namespace Gokt.Application.Commands.Auth.VerifyEmail;

public record ResendVerificationEmailCommand(string Email) : IRequest;

public sealed class ResendVerificationEmailCommandHandler(
    IUserRepository userRepository,
    ICacheService cacheService,
    ITokenService tokenService,
    IEmailService emailService) : IRequestHandler<ResendVerificationEmailCommand>
{
    private static readonly TimeSpan VerificationTtl = TimeSpan.FromMinutes(10);

    public async Task Handle(ResendVerificationEmailCommand cmd, CancellationToken ct)
    {
        var user = await userRepository.GetByEmailWithSecurityAsync(cmd.Email, ct)
            ?? throw new NotFoundException("User", cmd.Email);

        if (user.EmailVerified)
            throw new DomainException("ALREADY_VERIFIED", "Email address is already verified.");

        if (user.Status is UserStatus.Deleted or UserStatus.Suspended)
            throw new ForbiddenException("Account is not eligible for email verification.");

        var rawToken = tokenService.GenerateEmailVerificationCode();
        await cacheService.SetAsync(
            $"email_verify:{user.Id}",
            tokenService.HashToken(rawToken),
            VerificationTtl,
            ct);

        _ = emailService.SendVerificationEmailAsync(user.Email, rawToken, user.Id, CancellationToken.None);
    }
}
