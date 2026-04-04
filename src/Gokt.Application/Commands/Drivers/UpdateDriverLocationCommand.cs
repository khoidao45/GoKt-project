using FluentValidation;
using Gokt.Application.Interfaces;
using Gokt.Domain.Exceptions;
using MediatR;

namespace Gokt.Application.Commands.Drivers.UpdateDriverLocation;

public record UpdateDriverLocationCommand(Guid UserId, double Latitude, double Longitude) : IRequest;

public sealed class UpdateDriverLocationCommandValidator : AbstractValidator<UpdateDriverLocationCommand>
{
    public UpdateDriverLocationCommandValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
}

public sealed class UpdateDriverLocationCommandHandler(
    IDriverRepository driverRepository,
    ITripRepository tripRepository,
    ILocationService locationService,
    IRealtimeService realtimeService,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateDriverLocationCommand>
{
    public async Task Handle(UpdateDriverLocationCommand cmd, CancellationToken ct)
    {
        var driver = await driverRepository.GetByUserIdAsync(cmd.UserId, ct)
            ?? throw new NotFoundException("Driver", cmd.UserId);

        driver.UpdateLocation(cmd.Latitude, cmd.Longitude);
        await unitOfWork.SaveChangesAsync(ct);

        // Sync to Redis GEO
        var vehicleType = driver.Vehicles.FirstOrDefault()?.VehicleType.ToString() ?? "Economy";
        await locationService.UpdateDriverLocationAsync(
            driver.Id, cmd.Latitude, cmd.Longitude,
            driver.IsOnline, driver.IsBusy, vehicleType, ct);

        // Broadcast live location to the customer currently on a trip with this driver
        var activeTrip = await tripRepository.GetActiveByDriverIdAsync(driver.Id, ct);
        if (activeTrip is not null)
        {
            await realtimeService.BroadcastDriverLocationAsync(
                activeTrip.CustomerId,
                new DriverLocationPayload(driver.Id, cmd.Latitude, cmd.Longitude, DateTime.UtcNow),
                ct);
        }
    }
}
