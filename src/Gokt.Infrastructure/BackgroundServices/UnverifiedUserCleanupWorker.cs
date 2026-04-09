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
        var expired = await userRepo.GetExpiredUnverifiedAsync(cutoff, ct);
        if (expired.Count == 0) return;

        foreach (var user in expired)
            user.SoftDelete();

        await uow.SaveChangesAsync(ct);
        logger.LogInformation("UnverifiedUserCleanupWorker soft-deleted {Count} expired unverified account(s)", expired.Count);
    }
}
