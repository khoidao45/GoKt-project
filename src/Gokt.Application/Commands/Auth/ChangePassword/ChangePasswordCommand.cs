using FluentValidation;
using Gokt.Application.Interfaces;
using Gokt.Domain.Exceptions;
using MediatR;

namespace Gokt.Application.Commands.Auth.ChangePassword;

public record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : IRequest;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty().MinimumLength(8).MaximumLength(128)
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must differ from current password.")
            .Matches(@"[A-Z]").WithMessage("Must contain uppercase.")
            .Matches(@"[a-z]").WithMessage("Must contain lowercase.")
            .Matches(@"[0-9]").WithMessage("Must contain a digit.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Must contain a special character.");
    }
}

public sealed class ChangePasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ISessionRepository sessionRepository,
    IAuditService auditService,
    IUnitOfWork unitOfWork) : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand cmd, CancellationToken ct)
    {
        var user = await userRepository.GetByEmailWithSecurityAsync(
            (await userRepository.GetByIdAsync(cmd.UserId, ct))?.Email
            ?? throw new NotFoundException("User", cmd.UserId), ct)
            ?? throw new NotFoundException("User", cmd.UserId);

        if (!passwordHasher.Verify(cmd.CurrentPassword, user.PasswordHash!))
            throw new UnauthorizedException("Current password is incorrect.");

        user.SetPasswordHash(passwordHasher.Hash(cmd.NewPassword));

        // Revoke all sessions — new password means old tokens shouldn't work
        await sessionRepository.RevokeAllByUserIdAsync(user.Id, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogAsync("CHANGE_PASSWORD", user.Id, null, null, null, ct);
    }
}
