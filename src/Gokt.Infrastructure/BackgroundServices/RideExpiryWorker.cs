using Gokt.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gokt.Infrastructure.BackgroundServices;

/// <summary>
/// Safety-net worker that periodically scans for Searching rides that have passed their
/// ExpiresAt deadline (e.g., when MatchingService crashes or a race slips through).
/// </summary>
public class RideExpiryWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<RideExpiryWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("RideExpiryWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScanAndExpireAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RideExpiryWorker encountered an error during scan");
            }

            await Task.Delay(Interval, stoppingToken);
        }

        logger.LogInformation("RideExpiryWorker stopped");
    }

    private async Task ScanAndExpireAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var rideRepo = scope.ServiceProvider.GetRequiredService<IRideRequestRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var realtimeService = scope.ServiceProvider.GetRequiredService<IRealtimeService>();

        var expiredRides = (await rideRepo.GetExpiredSearchingAsync(ct)).ToList();
        if (expiredRides.Count == 0) return;

        logger.LogWarning("RideExpiryWorker: found {Count} expired Searching rides to clean up", expiredRides.Count);

        foreach (var ride in expiredRides)
        {
            try
            {
                ride.Expire();
                await uow.SaveChangesAsync(ct);
                await realtimeService.NotifyCustomerNoDriverFoundAsync(ride.CustomerId, ride.Id, ct);

                logger.LogWarning("RideExpiryWorker: expired ride {RideId} for customer {CustomerId}",
                    ride.Id, ride.CustomerId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RideExpiryWorker: failed to expire ride {RideId}", ride.Id);
            }
        }
    }
}
