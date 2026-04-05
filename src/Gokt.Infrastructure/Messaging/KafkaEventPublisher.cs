using System.Text.Json;
using Confluent.Kafka;
using Gokt.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gokt.Infrastructure.Messaging;

public class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;

    public KafkaEventPublisher(IOptions<KafkaOptions> options, ILogger<KafkaEventPublisher> logger)
    {
        _logger = logger;
        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            Acks = Acks.Leader,             // Leader ACK — balanced durability / latency
            MessageTimeoutMs = 5000,
            EnableIdempotence = true,       // Kafka producer-side idempotency
            MaxInFlight = 5
        };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    /// <inheritdoc/>
    public async Task PublishAsync<T>(string topic, T @event, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(@event);
        var key  = ExtractKey(@event);
        await ProduceAsync(topic, key, json, typeof(T).Name, ct);
    }

    /// <inheritdoc/>
    public async Task PublishRawAsync(string topic, string messageKey, string json, CancellationToken ct = default)
        => await ProduceAsync(topic, messageKey, json, "OutboxEvent", ct);

    // ── Internal ─────────────────────────────────────────────────────────────

    private async Task ProduceAsync(string topic, string key, string json, string logLabel, CancellationToken ct)
    {
        try
        {
            var result = await _producer.ProduceAsync(topic,
                new Message<string, string> { Key = key, Value = json }, ct);

            _logger.LogDebug(
                "Kafka produce: type={Label} topic={Topic} key={Key} partition={Partition} offset={Offset}",
                logLabel, topic, key, result.Partition.Value, result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Kafka produce failed: type={Label} topic={Topic} key={Key}", logLabel, topic, key);
            throw;
        }
    }

    private static string ExtractKey<T>(T @event)
    {
        try
        {
            // Best-effort: look for a RideRequestId or Id property
            dynamic? d = @event;
            return d?.RideRequestId?.ToString()
                ?? d?.Id?.ToString()
                ?? Guid.NewGuid().ToString();
        }
        catch
        {
            return Guid.NewGuid().ToString();
        }
    }

    public void Dispose() => _producer.Dispose();
}
