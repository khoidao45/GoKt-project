using Gokt.Domain.Enums;

namespace Gokt.Domain.Entities;

/// <summary>
/// Represents an event that must be published to Kafka, stored in the same DB transaction
/// as the business entity change. The OutboxProcessor picks these up asynchronously.
/// </summary>
public class OutboxEvent
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>Kafka topic (e.g., "ride.requested"). Used for routing by OutboxProcessor.</summary>
    public string Type { get; private set; } = default!;

    /// <summary>Pre-serialized JSON payload of the domain event.</summary>
    public string Payload { get; private set; } = default!;

    /// <summary>Kafka message key for partition locality (e.g., RideRequestId).</summary>
    public string MessageKey { get; private set; } = default!;

    public OutboxStatus Status { get; private set; } = OutboxStatus.Pending;
    public int RetryCount { get; private set; } = 0;
    public string? LastError { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; private set; }

    private OutboxEvent() { }

    public static OutboxEvent Create(string type, string messageKey, string payload) =>
        new()
        {
            Type       = type,
            MessageKey = messageKey,
            Payload    = payload
        };

    public void MarkProcessed()
    {
        Status      = OutboxStatus.Processed;
        ProcessedAt = DateTime.UtcNow;
        LastError   = null;
    }

    /// <summary>
    /// Increments retry counter. Caller decides whether to call <see cref="MarkFailed"/> after.
    /// </summary>
    public void IncrementRetry(string error)
    {
        RetryCount++;
        LastError = error;
    }

    public void MarkFailed(string error)
    {
        Status    = OutboxStatus.Failed;
        LastError = error;
    }

    /// <summary>
    /// Resets a Failed event back to Pending so OutboxProcessor will retry it.
    /// RetryCount is cleared so it gets another full window of retries.
    /// </summary>
    public void ResetForReplay()
    {
        Status      = OutboxStatus.Pending;
        RetryCount  = 0;
        LastError   = null;
        ProcessedAt = null;
    }
}
