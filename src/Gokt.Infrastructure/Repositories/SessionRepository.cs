using Gokt.Application.Interfaces;
using Gokt.Domain.Entities;
using Gokt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gokt.Infrastructure.Repositories;

public class SessionRepository(AppDbContext db) : ISessionRepository
{
    public async Task<UserSession?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        await db.UserSessions
            .FirstOrDefaultAsync(s => s.RefreshTokenHash == tokenHash, ct);

    public async Task<IEnumerable<UserSession>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await db.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastUsedAt)
            .ToListAsync(ct);

    public async Task AddAsync(UserSession session, CancellationToken ct = default) =>
        await db.UserSessions.AddAsync(session, ct);

    public async Task RevokeAllByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await db.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.IsRevoked, true)
                .SetProperty(x => x.RevokedAt, DateTime.UtcNow), ct);

    public async Task<UserSession?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.UserSessions.FindAsync([id], ct);
}
