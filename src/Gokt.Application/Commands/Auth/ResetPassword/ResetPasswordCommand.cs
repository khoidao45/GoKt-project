using FluentValidation;
using Gokt.Application.Interfaces;
using Gokt.Domain.Exceptions;
using MediatR;

namespace Gokt.Application.Commands.Auth.ResetPassword;

public record ResetPasswordCommand(string Token, string NewPassword) : IRequest;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty().MinimumLength(8).MaximumLength(128)
            .Matches(@"[A-Z]").WithMessage("Must contain uppercase.")
            .Matches(@"[a-z]").WithMessage("Must contain lowercase.")
            .Matches(@"[0-9]").WithMessage("Must contain a digit.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Must contain a special character.");
    }
}

public sealed class ResetPasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    ISessionRepository sessionRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand cmd, CancellationToken ct)
    {
        var tokenHash = tokenService.HashToken(cmd.Token);
        var users = await userRepository.GetAllAsync(1, int.MaxValue, ct);
        var user = users.FirstOrDefault(u => u.Security.IsPasswordResetTokenValid(tokenHash))
            ?? throw new DomainException("INVALID_TOKEN", "Reset token is invalid or has expired.");

        user.SetPasswordHash(passwordHasher.Hash(cmd.NewPassword));
        user.Security.ClearPasswordResetToken();

        // Revoke all sessions for security
        await sessionRepository.RevokeAllByUserIdAsync(user.Id, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
