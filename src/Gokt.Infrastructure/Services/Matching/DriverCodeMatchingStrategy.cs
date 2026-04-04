using Gokt.Application.Interfaces;
using Gokt.Domain.Enums;
using Gokt.Domain.Entities;
using Gokt.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using Gokt.Application.Commands.Rides.AcceptRideRequest;

namespace Gokt.Infrastructure.Services.Matching;

public class DriverCodeMatchingStrategy(
    IDriverRepository driverRepository,
    IMediator mediator,
    ILogger<DriverCodeMatchingStrategy> logger) : IMatchingStrategy
{
    public MatchingStrategyType StrategyType => MatchingStrategyType.DriverCode;

    public async Task<MatchResult> ExecuteAsync(MatchingContext context, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(context.DriverCode))
            return new MatchResult(false, FailureReason: "No driver code provided");

        logger.LogInformation(
            "DriverCode strategy: looking up code {DriverCode} for ride {RideId}",
            context.DriverCode, context.RideRequestId);

        var driver = await driverRepository.GetByDriverCodeAsync(context.DriverCode, ct);

        if (driver is null)
        {
            logger.LogWarning("DriverCode {Code} not found for ride {RideId}", context.DriverCode, context.RideRequestId);
            return new MatchResult(false, FailureReason: $"Driver with code '{context.DriverCode}' not found");
        }

        if (!driver.IsOnline)
        {
            logger.LogWarning("DriverCode {Code} driver {DriverId} is offline", context.DriverCode, driver.Id);
            return new MatchResult(false, FailureReason: "Requested driver is offline");
        }

        if (driver.IsBusy)
        {
            logger.LogWarning("DriverCode {Code} driver {DriverId} is busy", context.DriverCode, driver.Id);
            return new MatchResult(false, FailureReason: "Requested driver is currently on another trip");
        }

        // Directly dispatch AcceptRide — this creates the Trip with full idempotency protection
        try
        {
            var trip = await mediator.Send(
                new AcceptRideRequestCommand(driver.UserId, context.RideRequestId), ct);

            logger.LogInformation(
                "DriverCode direct assign: driver {DriverId} assigned to ride {RideId}, trip {TripId}",
                driver.Id, context.RideRequestId, trip.Id);

            return new MatchResult(true, AssignedDriverId: driver.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DriverCode assign failed for driver {DriverId} ride {RideId}",
                driver.Id, context.RideRequestId);
            return new MatchResult(false, FailureReason: ex.Message);
        }
    }
}
