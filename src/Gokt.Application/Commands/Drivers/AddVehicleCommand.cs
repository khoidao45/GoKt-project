using FluentValidation;
using Gokt.Application.DTOs;
using Gokt.Application.Interfaces;
using Gokt.Domain.Entities;
using Gokt.Domain.Enums;
using Gokt.Domain.Exceptions;
using MediatR;

namespace Gokt.Application.Commands.Drivers.AddVehicle;

public record AddVehicleCommand(
    Guid UserId,
    string Make,
    string Model,
    int Year,
    string Color,
    string PlateNumber,
    VehicleType VehicleType
) : IRequest<VehicleDto>;

public sealed class AddVehicleCommandValidator : AbstractValidator<AddVehicleCommand>
{
    public AddVehicleCommandValidator()
    {
        RuleFor(x => x.Make).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Year).InclusiveBetween(2000, DateTime.UtcNow.Year + 1);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(30);
        RuleFor(x => x.PlateNumber).NotEmpty().MaximumLength(20);
    }
}

public sealed class AddVehicleCommandHandler(
    IDriverRepository driverRepository,
    IVehicleRepository vehicleRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<AddVehicleCommand, VehicleDto>
{
    public async Task<VehicleDto> Handle(AddVehicleCommand cmd, CancellationToken ct)
    {
        var driver = await driverRepository.GetByUserIdAsync(cmd.UserId, ct)
            ?? throw new NotFoundException("Driver", cmd.UserId);

        if (await vehicleRepository.ExistsByPlateNumberAsync(cmd.PlateNumber, ct))
            throw new ConflictException("A vehicle with this plate number is already registered.");

        var vehicle = Vehicle.Create(
            driver.Id, cmd.Make, cmd.Model, cmd.Year,
            cmd.Color, cmd.PlateNumber, cmd.VehicleType);

        await vehicleRepository.AddAsync(vehicle, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return VehicleDto.From(vehicle);
    }
}
