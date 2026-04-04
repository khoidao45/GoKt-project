using FluentValidation;
using Gokt.Application.Interfaces;
using Gokt.Domain.Enums;
using Gokt.Domain.Exceptions;
using MediatR;

namespace Gokt.Application.Commands.Trips.RateTrip;

public record RateTripCommand(Guid UserId, Guid TripId, int Rating, string? Comment) : IRequest;

public sealed class RateTripCommandValidator : AbstractValidator<RateTripCommand>
{
    public RateTripCommandValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Comment).MaximumLength(500).When(x => x.Comment != null);
    }
}

public sealed class RateTripCommandHandler(
    IDriverRepository driverRepository,
    ITripRepository tripRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<RateTripCommand>
{
    public async Task Handle(RateTripCommand cmd, CancellationToken ct)
    {
        var trip = await tripRepository.GetByIdAsync(cmd.TripId, ct)
            ?? throw new NotFoundException("Trip", cmd.TripId);

        if (trip.Status != TripStatus.Completed)
            throw new ConflictException("Only completed trips can be rated.");

        var driver = await driverRepository.GetByUserIdAsync(cmd.UserId, ct);

        if (driver is not null && driver.Id == trip.DriverId)
        {
            if (trip.DriverRating.HasValue)
                throw new ConflictException("You have already rated this trip.");

            trip.RateByDriver(cmd.Rating, cmd.Comment);

            // Recalculate customer's driver rating (simple rolling average)
            var newDriverRating = (driver.Rating * driver.TotalRides + cmd.Rating) / (driver.TotalRides + 1);
            driver.UpdateRating((decimal)newDriverRating);
        }
        else if (trip.CustomerId == cmd.UserId)
        {
            if (trip.CustomerRating.HasValue)
                throw new ConflictException("You have already rated this trip.");

            trip.RateByCustomer(cmd.Rating, cmd.Comment);
        }
        else
        {
            throw new ForbiddenException("You are not a participant in this trip.");
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}
