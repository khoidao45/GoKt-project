using Gokt.Application.Interfaces;
using Gokt.Domain.Exceptions;
using MediatR;

namespace Gokt.Application.Commands.Auth.VerifyEmail;

public record VerifyEmailCommand(Guid UserId, string Token) : IRequest;

public sealed class VerifyEmailCommandHandler(
    IUserRepository userRepository,
    ICacheService cacheService,
    ITokenService tokenService,
    IUnitOfWork unitOfWork) : IRequestHandler<VerifyEmailCommand>
{
    public async Task Handle(VerifyEmailCommand cmd, CancellationToken ct)
    {
        // Lookup stored token hash from Redis (key expires after 10 minutes)
        var storedHash = await cacheService.GetAsync<string>($"email_verify:{cmd.UserId}", ct);

        if (storedHash is null)
            throw new DomainException("EXPIRED_TOKEN", "Verification token has expired or is invalid. Please request a new verification email.");

        var user = await userRepository.GetByIdAsync(cmd.UserId, ct)
            ?? throw new NotFoundException("User", cmd.UserId);

        if (user.EmailVerified)
            throw new DomainException("ALREADY_VERIFIED", "Email address is already verified.");

        if (tokenService.HashToken(cmd.Token) != storedHash)
            throw new DomainException("INVALID_TOKEN", "Verification token is invalid.");

        user.VerifyEmail();
        await unitOfWork.SaveChangesAsync(ct);

        // Delete the token immediately — single-use
        await cacheService.RemoveAsync($"email_verify:{cmd.UserId}", ct);
    }
}
