using Gokt.Domain.Entities;

namespace Gokt.Application.Interfaces;

public interface IOutboxRepository
{
    /// <summary>Stages an outbox event for persistence (does NOT call SaveChanges).</summary>
    Task AddAsync(OutboxEvent outboxEvent, CancellationToken ct = default);

    /// <summary>
    /// Returns a batch of Pending events, locked with FOR UPDATE SKIP LOCKED.
    /// Must be called inside an open database transaction.
    /// </summary>
    Task<IReadOnlyList<OutboxEvent>> GetPendingBatchAsync(int batchSize, CancellationToken ct = default);

    /// <summary>Returns a page of Failed events for the admin dashboard / replay UI.</summary>
    Task<IReadOnlyList<OutboxEvent>> GetFailedAsync(int skip, int take, CancellationToken ct = default);

    /// <summary>Counts Failed events — used for monitoring/alerting endpoints.</summary>
    Task<int> CountFailedAsync(CancellationToken ct = default);

    /// <summary>Loads a single event by ID (for targeted replay).</summary>
    Task<OutboxEvent?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Resets all Failed events to Pending so they are re-picked by OutboxProcessor.
    /// Returns the number of events reset.
    /// </summary>
    Task<int> ReplayAllFailedAsync(CancellationToken ct = default);
}
