using System.Text.Json;
using Gokt.Application.Interfaces;
using Gokt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gokt.Infrastructure.BackgroundServices;

/// <summary>
/// Polls OutboxEvents WHERE Status='Pending', publishes each to its Kafka topic,
/// and updates status atomically inside a PostgreSQL transaction.
///
/// Guarantees:
///   • FOR UPDATE SKIP LOCKED  → safe to run on multiple instances in parallel.
///   • At-least-once delivery  → consumers must be idempotent.
///   • After MaxRetries failures → publishes a Dead Letter to ride.failed and marks Failed.
/// </summary>
public class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    private const int BatchSize  = 20;
    private const int MaxRetries = 5;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "OutboxProcessor started (batch={Batch}, interval={Interval}s, maxRetries={Max})",
            BatchSize, PollingInterval.TotalSeconds, MaxRetries);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OutboxProcessor: unhandled error during batch processing");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }

        logger.LogInformation("OutboxProcessor stopped");
    }

    // ── Batch processing ──────────────────────────────────────────────────────

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope    = scopeFactory.CreateScope();
        var db             = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var outboxRepo     = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var publisher      = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var strategy = db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            var events = await outboxRepo.GetPendingBatchAsync(BatchSize, ct);
            if (events.Count == 0)
            {
                await tx.RollbackAsync(ct);
                return;
            }

            logger.LogInformation("OutboxProcessor: processing {Count} event(s)", events.Count);

            foreach (var outboxEvent in events)
            {
                try
                {
                    await publisher.PublishRawAsync(
                        outboxEvent.Type,
                        outboxEvent.MessageKey,
                        outboxEvent.Payload,
                        ct);

                    outboxEvent.MarkProcessed();

                    logger.LogInformation(
                        "OutboxProcessor: published {Id} → topic={Type} key={Key}",
                        outboxEvent.Id, outboxEvent.Type, outboxEvent.MessageKey);
                }
                catch (Exception ex)
                {
                    outboxEvent.IncrementRetry(ex.Message);

                    if (outboxEvent.RetryCount >= MaxRetries)
                    {
                        // ── DEAD LETTER ──────────────────────────────────────────
                        // Best-effort: publish to DLQ so the event is observable
                        // and can be replayed from outside the system (Kafka tooling,
                        // admin endpoint, etc.).
                        await TrySendToDlqAsync(publisher, outboxEvent, ex.Message, ct);
                        outboxEvent.MarkFailed(ex.Message);

                        logger.LogError(ex,
                            "OutboxProcessor: event {Id} (type={Type} key={Key}) exhausted {Max} retries — DLQ'd and marked Failed",
                            outboxEvent.Id, outboxEvent.Type, outboxEvent.MessageKey, MaxRetries);
                    }
                    else
                    {
                        logger.LogWarning(ex,
                            "OutboxProcessor: event {Id} publish failed — retry {Retry}/{Max}",
                            outboxEvent.Id, outboxEvent.RetryCount, MaxRetries);
                    }
                }
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });
    }

    // ── DLQ helper ────────────────────────────────────────────────────────────

    private async Task TrySendToDlqAsync(
        IEventPublisher publisher,
        Gokt.Domain.Entities.OutboxEvent outboxEvent,
        string error,
        CancellationToken ct)
    {
        try
        {
            var dlqPayload = JsonSerializer.Serialize(new DeadLetterMessage(
                OriginalEventId:   outboxEvent.Id,
                OriginalTopic:     outboxEvent.Type,
                OriginalMessageKey: outboxEvent.MessageKey,
                OriginalPayload:   outboxEvent.Payload,
                FailureReason:     error,
                RetryCount:        outboxEvent.RetryCount,
                FailedAt:          DateTime.UtcNow));

            await publisher.PublishRawAsync(
                KafkaTopics.RideFailed,       // "ride.failed"
                outboxEvent.MessageKey,
                dlqPayload,
                ct);

            logger.LogWarning(
                "OutboxProcessor: sent event {Id} to DLQ topic '{Dlq}'",
                outboxEvent.Id, KafkaTopics.RideFailed);
        }
        catch (Exception dlqEx)
        {
            // DLQ failure is non-fatal — the event is still marked Failed in DB.
            // The admin endpoint / ReplayAllFailed can recover it later.
            logger.LogError(dlqEx,
                "OutboxProcessor: ALSO failed to send event {Id} to DLQ — event only in DB as Failed",
                outboxEvent.Id);
        }
    }
}

// ── DLQ message envelope ──────────────────────────────────────────────────────

/// <summary>
/// Envelope published to the Dead Letter topic (ride.failed).
/// Consumers of the DLQ topic receive this and can alert/replay.
/// </summary>
public record DeadLetterMessage(
    Guid     OriginalEventId,
    string   OriginalTopic,
    string   OriginalMessageKey,
    string   OriginalPayload,
    string   FailureReason,
    int      RetryCount,
    DateTime FailedAt);
