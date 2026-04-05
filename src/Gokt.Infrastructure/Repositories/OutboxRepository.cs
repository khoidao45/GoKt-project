using Gokt.Application.Interfaces;
using Gokt.Domain.Entities;
using Gokt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gokt.Infrastructure.Repositories;

public class OutboxRepository(AppDbContext db) : IOutboxRepository
{
    public Task AddAsync(OutboxEvent outboxEvent, CancellationToken ct = default)
    {
        db.OutboxEvents.Add(outboxEvent);
        return Task.CompletedTask; // Actual save happens via IUnitOfWork.SaveChangesAsync
    }

    /// <summary>
    /// Fetches a batch of pending events using PostgreSQL's FOR UPDATE SKIP LOCKED
    /// so multiple OutboxProcessor instances never process the same row.
    /// Must be called inside an open transaction.
    /// </summary>
    public Task<IReadOnlyList<OutboxEvent>> GetPendingBatchAsync(int batchSize, CancellationToken ct = default)
    {
        // Raw SQL is used because EF Core does not expose SKIP LOCKED in its LINQ API.
        // The processor opens a transaction before calling this method, which scopes the lock.
        return db.OutboxEvents
            .FromSqlRaw(
                """
                SELECT * FROM "OutboxEvents"
                WHERE  "Status" = 'Pending'
                ORDER  BY "CreatedAt"
                LIMIT  {0}
                FOR UPDATE SKIP LOCKED
                """,
                batchSize)
            .AsTracking()                    // We need change-tracking to update Status later
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<OutboxEvent>)t.Result, ct,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
    }
}
