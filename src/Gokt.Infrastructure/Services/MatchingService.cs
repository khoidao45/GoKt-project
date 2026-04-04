using Gokt.Application.Interfaces;
using Gokt.Domain.Enums;
using Gokt.Infrastructure.Services.Matching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gokt.Infrastructure.Services;

public class MatchingService(
    IServiceScopeFactory scopeFactory,
    ILogger<MatchingService> logger) : IMatchingService
{
    public Task StartMatchingAsync(Guid rideRequestId, CancellationToken ct = default)
    {
        // Fire-and-forget — creates its own scope so it outlives the HTTP request
        _ = Task.Run(() => RunMatchingAsync(rideRequestId), CancellationToken.None);
        return Task.CompletedTask;
    }

    private async Task RunMatchingAsync(Guid rideRequestId)
    {
        using var scope = scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        var rideRequestRepo = sp.GetRequiredService<IRideRequestRepository>();
        var locationService = sp.GetRequiredService<ILocationService>();
        var realtimeService = sp.GetRequiredService<IRealtimeService>();
        var unitOfWork = sp.GetRequiredService<IUnitOfWork>();

        try
        {
            var rideRequest = await rideRequestRepo.GetByIdAsync(rideRequestId);
            if (rideRequest is null)
            {
                logger.LogWarning("Matching: ride {RideId} not found", rideRequestId);
                return;
            }

            var vehicleType = rideRequest.RequestedVehicleType.ToString();
            var context = new MatchingContext(
                rideRequestId,
                rideRequest.PickupLatitude,
                rideRequest.PickupLongitude,
                vehicleType,
                rideRequest.CustomerId,
                rideRequest.DriverCode);

            // Select strategy
            IMatchingStrategy strategy;
            if (!string.IsNullOrWhiteSpace(rideRequest.DriverCode))
            {
                var driverCodeStrategy = sp.GetRequiredService<DriverCodeMatchingStrategy>();
                strategy = driverCodeStrategy;
                logger.LogInformation("Matching: ride {RideId} using DriverCode strategy (code={Code})",
                    rideRequestId, rideRequest.DriverCode);
            }
            else
            {
                var autoStrategy = sp.GetRequiredService<AutoMatchingStrategy>();
                strategy = autoStrategy;
                logger.LogInformation("Matching: ride {RideId} using Auto (wave) strategy", rideRequestId);
            }

            var result = await strategy.ExecuteAsync(context, CancellationToken.None);

            if (!result.Success && strategy.StrategyType == MatchingStrategyType.DriverCode)
            {
                // Driver code failed — fall back to Auto strategy
                logger.LogWarning(
                    "DriverCode strategy failed for ride {RideId}: {Reason}. Falling back to Auto.",
                    rideRequestId, result.FailureReason);

                var autoStrategy = sp.GetRequiredService<AutoMatchingStrategy>();
                result = await autoStrategy.ExecuteAsync(context, CancellationToken.None);
            }

            if (result.Success)
                logger.LogInformation("Matching completed successfully for ride {RideId}", rideRequestId);
            else
                logger.LogWarning("Matching failed for ride {RideId}: {Reason}", rideRequestId, result.FailureReason);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error in matching for ride {RideId}", rideRequestId);
        }
    }
}
