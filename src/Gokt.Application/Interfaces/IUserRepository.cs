using Gokt.Domain.Entities;

namespace Gokt.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByIdWithRolesAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByEmailWithSecurityAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByPhoneAsync(string phone, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task<IEnumerable<User>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetExpiredUnverifiedAsync(DateTime cutoffUtc, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetExpiredDeletedUnverifiedAsync(DateTime cutoffUtc, CancellationToken ct = default);
    Task RemoveRangeAsync(IEnumerable<User> users, CancellationToken ct = default);
}
