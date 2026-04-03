using Gokt.Application.Interfaces;
using MediatR;

namespace Gokt.Application.Commands.Auth.ForgotPassword;

public record ForgotPasswordCommand(string Email) : IRequest;

public sealed class ForgotPasswordCommandHandler(
    IUserRepository userRepository,
    ITokenService tokenService,
    IEmailService emailService,
    IUnitOfWork unitOfWork) : IRequestHandler<ForgotPasswordCommand>
{
    public async Task Handle(ForgotPasswordCommand cmd, CancellationToken ct)
    {
        // Always return success to prevent email enumeration
        var user = await userRepository.GetByEmailWithSecurityAsync(cmd.Email, ct);
        if (user is null) return;

        var rawToken = tokenService.GenerateSecureToken();
        user.Security.SetPasswordResetToken(tokenService.HashToken(rawToken));

        await unitOfWork.SaveChangesAsync(ct);
        _ = emailService.SendPasswordResetEmailAsync(user.Email, rawToken);
    }
}
