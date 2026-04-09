using Gokt.Application.Interfaces;
using Gokt.Domain.Enums;
using Gokt.Infrastructure.Resilience;
using Microsoft.Extensions.Logging;

namespace Gokt.Infrastructure.Services.Matching;

public class AutoMatchingStrategy(
    ILocationService locationService,
    IRealtimeService realtimeService,
    IDriverRepository driverRepository,
    IRideRequestRepository rideRequestRepository,
    IUnitOfWork unitOfWork,
    ILogger<AutoMatchingStrategy> logger) : IMatchingStrategy
{
    public MatchingStrategyType StrategyType => MatchingStrategyType.Auto;

    private static readonly TimeSpan MatchPollInterval = TimeSpan.FromSeconds(3);

    private static readonly (double RadiusKm, int MaxDrivers, int WaitSeconds)[] Waves =
    [
        (5,  5,  20),
        (7,  10, 20),
        (10, 20, 20),
    ];

    public async Task<MatchResult> ExecuteAsync(MatchingContext context, CancellationToken ct)
    {
        var notifiedSet = new HashSet<Guid>();

        for (var waveIndex = 0; waveIndex < Waves.Length; waveIndex++)
        {
            var (radiusKm, maxDrivers, waitSeconds) = Waves[waveIndex];
            logger.LogInformation(
                "Matching wave {Wave} for ride {RideId}: radius={Radius}km maxDrivers={Max}",
                waveIndex + 1, context.RideRequestId, radiusKm, maxDrivers);

            var allCandidates = await ResiliencePolicies.RedisRetry.ExecuteAsync(() =>
                locationService.GetNearbyAvailableDriversAsync(
                    context.PickupLat, context.PickupLng, radiusKm, context.VehicleType, ct));

            // Filter out already-notified and drivers in cooldown; then score remaining
            var scoredCandidates = new List<(Guid DriverId, double Score)>();
            foreach (var driverId in allCandidates.Where(id => !notifiedSet.Contains(id)))
            {
                if (await locationService.IsDriverInCooldownAsync(driverId, ct))
                    continue;

                var driver = await driverRepository.GetByIdAsync(driverId, ct);
                if (driver is null) continue;

                // Score: normalized rating (0-1) + proximity bonus (wave-relative, approximated as flat boost)
                var score = driver.Rating / 5.0m;
                scoredCandidates.Add((driverId, (double)score));
            }

            var newCandidates = scoredCandidates
                .OrderByDescending(x => x.Score)
                .Take(maxDrivers)
                .Select(x => x.DriverId)
                .ToList();

            if (newCandidates.Count == 0)
            {
                logger.LogInformation("Wave {Wave}: no new candidates for ride {RideId}", waveIndex + 1, context.RideRequestId);
            }
            else
            {
                await ResiliencePolicies.RedisRetry.ExecuteAsync(() =>
                    locationService.SetRideCandidatesAsync(
                        context.RideRequestId, newCandidates, TimeSpan.FromSeconds(90), ct));

                var rideRequest = await rideRequestRepository.GetByIdAsync(context.RideRequestId, ct);
                if (rideRequest is null) return new MatchResult(false, FailureReason: "Ride not found");

                var offer = new RideOfferPayload(
                    context.RideRequestId,
                    context.PickupLat, context.PickupLng, rideRequest.PickupAddress,
                    rideRequest.DropoffLatitude, rideRequest.DropoffLongitude, rideRequest.DropoffAddress,
                    context.VehicleType,
                    rideRequest.EstimatedFare,
                    rideRequest.EstimatedDistanceKm,
                    rideRequest.ExpiresAt);

                foreach (var driverId in newCandidates)
                {
                    var connIds = await locationService.GetDriverConnectionsAsync(driverId, ct);
                    if (connIds.Count > 0)
                    {
                        await realtimeService.NotifyDriverOfRideAsync(connIds, offer, ct);
                        logger.LogDebug("Wave {Wave}: notified driver {DriverId} on {Count} connection(s)",
                            waveIndex + 1, driverId, connIds.Count);
                    }
                    notifiedSet.Add(driverId);
                }
            }

            // Poll every 3s during the wave to catch user acceptance/cancellation quickly.
            var elapsed = TimeSpan.Zero;
            var waveDuration = TimeSpan.FromSeconds(waitSeconds);
            while (elapsed < waveDuration)
            {
                var remaining = waveDuration - elapsed;
                var delay = remaining < MatchPollInterval ? remaining : MatchPollInterval;
                await Task.Delay(delay, CancellationToken.None);
                elapsed += delay;

                var current = await rideRequestRepository.GetByIdAsync(context.RideRequestId, ct);
                if (current is null) return new MatchResult(false, FailureReason: "Ride not found after wait");

                if (current.Status == RideStatus.Accepted)
                {
                    logger.LogInformation("Matching success: ride {RideId} accepted after wave {Wave}",
                        context.RideRequestId, waveIndex + 1);
                    return new MatchResult(true);
                }

                if (current.Status is RideStatus.Cancelled or RideStatus.Expired)
                    return new MatchResult(false, FailureReason: "Ride cancelled or expired during matching");
            }
        }

        // All waves exhausted
        logger.LogWarning("Matching failed: no driver found for ride {RideId} after {WaveCount} waves",
            context.RideRequestId, Waves.Length);

        var finalRide = await rideRequestRepository.GetByIdAsync(context.RideRequestId, ct);
        if (finalRide is not null && finalRide.Status == RideStatus.Searching)
        {
            finalRide.Expire();
            await unitOfWork.SaveChangesAsync(ct);
            await realtimeService.NotifyCustomerNoDriverFoundAsync(context.CustomerId, context.RideRequestId, ct);
        }

        return new MatchResult(false, FailureReason: "No drivers found after all waves");
    }
}
