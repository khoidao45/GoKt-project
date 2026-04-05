using System.Text.Json;
using Confluent.Kafka;
using Gokt.Application.Events;
using Gokt.Application.Interfaces;
using Gokt.Infrastructure.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Gokt.MatchingWorker.Workers;

public class KafkaMatchingConsumer(
    IOptions<KafkaOptions> options,
    IServiceScopeFactory scopeFactory,
    ILogger<KafkaMatchingConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("KafkaMatchingConsumer started. BootstrapServers={Servers} GroupId={GroupId}",
            options.Value.BootstrapServers, options.Value.GroupId);

        var config = new ConsumerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            GroupId = options.Value.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,   // Manual commit — at-least-once delivery
            SessionTimeoutMs = 30000,
            HeartbeatIntervalMs = 3000,
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) =>
                logger.LogError("Kafka consumer error: {Reason} (IsFatal={IsFatal})", e.Reason, e.IsFatal))
            .Build();

        consumer.Subscribe(KafkaTopics.RideRequested);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);
                if (consumeResult is null) continue;

                logger.LogInformation("Consumed ride.requested message at partition {Partition} offset {Offset}",
                    consumeResult.Partition.Value, consumeResult.Offset.Value);

                var rideEvent = JsonSerializer.Deserialize<RideRequestedEvent>(consumeResult.Message.Value);
                if (rideEvent is null)
                {
                    logger.LogWarning("Failed to deserialize ride.requested message — skipping");
                    consumer.Commit(consumeResult);
                    continue;
                }

                await ProcessAsync(rideEvent, stoppingToken);

                // Commit only after successful processing (at-least-once)
                consumer.Commit(consumeResult);
            }
            catch (ConsumeException ex)
            {
                logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
                // Brief pause before retrying to avoid tight loop on persistent errors
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error processing Kafka message");
            }
        }

        consumer.Close();
        logger.LogInformation("KafkaMatchingConsumer stopped");
    }

    private async Task ProcessAsync(RideRequestedEvent rideEvent, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();

        logger.LogInformation("Starting matching for ride {RideRequestId} (customer {CustomerId})",
            rideEvent.RideRequestId, rideEvent.CustomerId);

        try
        {
            await matchingService.StartMatchingAsync(rideEvent.RideRequestId, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Matching failed for ride {RideRequestId}", rideEvent.RideRequestId);
        }
    }
}
