namespace Gokt.Application.Interfaces;

public interface IEventPublisher
{
    /// <summary>Serializes <paramref name="event"/> to JSON and publishes to Kafka.</summary>
    Task PublishAsync<T>(string topic, T @event, CancellationToken ct = default);

    /// <summary>
    /// Publishes a pre-serialized JSON payload directly to Kafka.
    /// Used by the OutboxProcessor, which stores events already serialized.
    /// </summary>
    Task PublishRawAsync(string topic, string messageKey, string json, CancellationToken ct = default);
}

public static class KafkaTopics
{
    public const string RideRequested = "ride.requested";
    public const string RideAccepted  = "ride.accepted";
    public const string RideExpired   = "ride.expired";
    public const string RideFailed    = "ride.failed";
}
