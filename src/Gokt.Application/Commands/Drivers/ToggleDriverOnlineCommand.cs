using Gokt.Application.Interfaces;
using Gokt.Domain.Exceptions;
using MediatR;

namespace Gokt.Application.Commands.Drivers.ToggleDriverOnline;

public record ToggleDriverOnlineCommand(Guid UserId, bool IsOnline) : IRequest;

public sealed class ToggleDriverOnlineCommandHandler(
    IDriverRepository driverRepository,
    IVehicleRepository vehicleRepository,
    ILocationService locationService,
    IUnitOfWork unitOfWork) : IRequestHandler<ToggleDriverOnlineCommand>
{
    public async Task Handle(ToggleDriverOnlineCommand cmd, CancellationToken ct)
    {
        var driver = await driverRepository.GetByUserIdAsync(cmd.UserId, ct)
            ?? throw new NotFoundException("Driver", cmd.UserId);

        if (cmd.IsOnline)
        {
            var activeVehicle = await vehicleRepository.GetActiveByDriverIdAsync(driver.Id, ct);
            if (activeVehicle is null)
                throw new ForbiddenException("You must have at least one active vehicle to go online.");

            driver.GoOnline();
            await unitOfWork.SaveChangesAsync(ct);

            // Sync to Redis using last known location (if available)
            if (driver.CurrentLatitude.HasValue && driver.CurrentLongitude.HasValue)
            {
                await locationService.UpdateDriverLocationAsync(
                    driver.Id,
                    driver.CurrentLatitude.Value,
                    driver.CurrentLongitude.Value,
                    isOnline: true,
                    isBusy: driver.IsBusy,
                    vehicleType: activeVehicle.VehicleType.ToString(),
                    ct);
            }
        }
        else
        {
            driver.GoOffline();
            await unitOfWork.SaveChangesAsync(ct);

            // Remove from Redis GEO index + clear status
            await locationService.RemoveDriverAsync(driver.Id, ct);
        }
    }
}
