using FluentValidation;
using Gokt.Application.DTOs;
using Gokt.Application.Interfaces;
using Gokt.Domain.Entities;
using Gokt.Domain.Exceptions;
using MediatR;

namespace Gokt.Application.Commands.Drivers.RegisterDriver;

public record RegisterDriverCommand(
    Guid UserId,
    string LicenseNumber,
    DateTime LicenseExpiry
) : IRequest<DriverDto>;

public sealed class RegisterDriverCommandValidator : AbstractValidator<RegisterDriverCommand>
{
    public RegisterDriverCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.LicenseNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LicenseExpiry).GreaterThan(DateTime.UtcNow).WithMessage("License must not be expired.");
    }
}

public sealed class RegisterDriverCommandHandler(
    IDriverRepository driverRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<RegisterDriverCommand, DriverDto>
{
    public async Task<DriverDto> Handle(RegisterDriverCommand cmd, CancellationToken ct)
    {
        if (await driverRepository.ExistsByUserIdAsync(cmd.UserId, ct))
            throw new ConflictException("Driver profile already exists for this user.");

        if (await driverRepository.ExistsByLicenseAsync(cmd.LicenseNumber, ct))
            throw new ConflictException("License number is already registered.");

        var user = await userRepository.GetByIdWithRolesAsync(cmd.UserId, ct)
            ?? throw new NotFoundException("User", cmd.UserId);

        var driver = Driver.Create(cmd.UserId, cmd.LicenseNumber, cmd.LicenseExpiry);
        await driverRepository.AddAsync(driver, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var fullName = user.Profile != null
            ? $"{user.Profile.FirstName} {user.Profile.LastName}".Trim()
            : user.Email;

        return DriverDto.From(driver, fullName);
    }
}
