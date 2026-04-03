using Gokt.Domain.Entities;

namespace Gokt.Application.Interfaces;

public interface ISessionRepository
{
    Task<UserSession?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task<IEnumerable<UserSession>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(UserSession session, CancellationToken ct = default);
    Task RevokeAllByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<UserSession?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
