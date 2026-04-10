using FluentValidation;
using Gokt.Application.DTOs;
using Gokt.Application.Interfaces;
using Gokt.Domain.Enums;
using Gokt.Domain.Exceptions;
using MediatR;

namespace Gokt.Application.Commands.Drivers.RequestVehicleUpdate;

public record RequestVehicleUpdateCommand(
    Guid UserId,
    Guid VehicleId,
    string Make,
    string Model,
    int Year,
    string Color,
    string PlateNumber,
    int SeatCount,
    string? ImageUrl,
    VehicleType VehicleType
) : IRequest<VehicleDto>;

public sealed class RequestVehicleUpdateCommandValidator : AbstractValidator<RequestVehicleUpdateCommand>
{
    private static readonly HashSet<string> AllowedElectricModels = new(StringComparer.OrdinalIgnoreCase)
    {
        "VF 5",
        "VF 6",
        "VF 7",
        "VF 8",
        "VF 9",
        "VF e34",
        "Evo200",
        "Feliz S",
        "Klara S",
        "Theon S"
    };

    public RequestVehicleUpdateCommandValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.Make).NotEmpty().MaximumLength(50)
            .Must(m => string.Equals(m.Trim(), "VinFast", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only VinFast electric vehicles are allowed in this fleet.");
        RuleFor(x => x.Model).NotEmpty().MaximumLength(50)
            .Must(model => AllowedElectricModels.Contains(model.Trim()))
            .WithMessage("Model is not in the approved electric fleet list.");
        RuleFor(x => x.Year).InclusiveBetween(2000, DateTime.UtcNow.Year + 1);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(30);
        RuleFor(x => x.PlateNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.SeatCount).Must(v => v is 1 or 4 or 7 or 9)
            .WithMessage("Seat count must be one of 1, 4, 7, or 9.");
        RuleFor(x => x.ImageUrl).MaximumLength(500);

        RuleFor(x => x).Custom((x, context) =>
        {
            if (x.VehicleType == VehicleType.ElectricBike && x.SeatCount != 1)
                context.AddFailure(nameof(x.VehicleType), "ElectricBike must use seat count 1.");
            if (x.VehicleType == VehicleType.Seat4 && x.SeatCount != 4)
                context.AddFailure(nameof(x.VehicleType), "Seat4 type must use seat count 4.");
            if (x.VehicleType == VehicleType.Seat7 && x.SeatCount != 7)
                context.AddFailure(nameof(x.VehicleType), "Seat7 type must use seat count 7.");
            if (x.VehicleType == VehicleType.Seat9 && x.SeatCount != 9)
                context.AddFailure(nameof(x.VehicleType), "Seat9 type must use seat count 9.");
        });
    }
}

public sealed class RequestVehicleUpdateCommandHandler(
    IDriverRepository driverRepository,
    IVehicleRepository vehicleRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<RequestVehicleUpdateCommand, VehicleDto>
{
    public async Task<VehicleDto> Handle(RequestVehicleUpdateCommand cmd, CancellationToken ct)
    {
        var driver = await driverRepository.GetByUserIdAsync(cmd.UserId, ct)
            ?? throw new NotFoundException("Driver", cmd.UserId);

        var vehicle = await vehicleRepository.GetByIdAsync(cmd.VehicleId, ct)
            ?? throw new NotFoundException("Vehicle", cmd.VehicleId);

        if (vehicle.DriverId != driver.Id)
            throw new ForbiddenException("This vehicle does not belong to you.");

        if (await vehicleRepository.ExistsByPlateNumberExceptAsync(cmd.PlateNumber, cmd.VehicleId, ct))
            throw new ConflictException("A vehicle with this plate number is already registered.");

        vehicle.RequestUpdate(
            cmd.Make,
            cmd.Model,
            cmd.Year,
            cmd.Color,
            cmd.PlateNumber,
            cmd.SeatCount,
            cmd.VehicleType,
            cmd.ImageUrl);

        await vehicleRepository.UpdateAsync(vehicle, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return VehicleDto.From(vehicle);
    }
}
