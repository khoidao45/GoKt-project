using Gokt.Application.Interfaces;
using Gokt.Domain.Exceptions;
using MediatR;

namespace Gokt.Application.Commands.Auth.VerifyEmail;

public record VerifyEmailByTokenCommand(string Email, string Token) : IRequest;

public sealed class VerifyEmailByTokenCommandHandler(
    IUserRepository userRepository,
    ICacheService cacheService,
    ITokenService tokenService,
    IUnitOfWork unitOfWork) : IRequestHandler<VerifyEmailByTokenCommand>
{
    public async Task Handle(VerifyEmailByTokenCommand cmd, CancellationToken ct)
    {
        var user = await userRepository.GetByEmailWithSecurityAsync(cmd.Email, ct)
            ?? throw new NotFoundException("User", cmd.Email);

        var storedHash = await cacheService.GetAsync<string>($"email_verify:{user.Id}", ct);
        if (storedHash is null)
            throw new DomainException("EXPIRED_TOKEN", "Verification token has expired or is invalid. Please request a new verification email.");

        if (user.EmailVerified)
            throw new DomainException("ALREADY_VERIFIED", "Email address is already verified.");

        if (tokenService.HashToken(cmd.Token) != storedHash)
            throw new DomainException("INVALID_TOKEN", "Verification token is invalid.");

        user.VerifyEmail();
        await unitOfWork.SaveChangesAsync(ct);
        await cacheService.RemoveAsync($"email_verify:{user.Id}", ct);
    }
}
