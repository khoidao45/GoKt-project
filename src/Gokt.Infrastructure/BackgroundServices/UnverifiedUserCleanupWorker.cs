using Gokt.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gokt.Infrastructure.BackgroundServices;

public class UnverifiedUserCleanupWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<UnverifiedUserCleanupWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan UnverifiedTtl = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("UnverifiedUserCleanupWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "UnverifiedUserCleanupWorker encountered an error");
            }

            await Task.Delay(Interval, stoppingToken);
        }

        logger.LogInformation("UnverifiedUserCleanupWorker stopped");
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var cutoff = DateTime.UtcNow - UnverifiedTtl;
        var expiredPending = await userRepo.GetExpiredUnverifiedAsync(cutoff, ct);
        foreach (var user in expiredPending)
            user.SoftDelete();

        var purgeableDeleted = await userRepo.GetExpiredDeletedUnverifiedAsync(cutoff, ct);
        if (purgeableDeleted.Count > 0)
            await userRepo.RemoveRangeAsync(purgeableDeleted, ct);

        if (expiredPending.Count == 0 && purgeableDeleted.Count == 0) return;

        await uow.SaveChangesAsync(ct);
        logger.LogInformation(
            "UnverifiedUserCleanupWorker processed expired unverified accounts: soft-deleted {SoftDeletedCount}, hard-deleted {HardDeletedCount}",
            expiredPending.Count,
            purgeableDeleted.Count);
    }
}
