using Gokt.Application.Interfaces;
using Gokt.Domain.Entities;
using Gokt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gokt.Infrastructure.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null, ct);

    public async Task<User?> GetByIdWithRolesAsync(Guid id, CancellationToken ct = default) =>
        await db.Users
            .Include(u => u.Profile)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await db.Users
            .Include(u => u.Profile)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant() && u.DeletedAt == null, ct);

    public async Task<User?> GetByEmailWithSecurityAsync(string email, CancellationToken ct = default) =>
        await db.Users
            .Include(u => u.Security)
            .Include(u => u.Profile)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant() && u.DeletedAt == null, ct);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        await db.Users.AnyAsync(u => u.Email == email.ToLowerInvariant() && u.DeletedAt == null, ct);

    public async Task<bool> ExistsByPhoneAsync(string phone, CancellationToken ct = default) =>
        await db.Users.AnyAsync(u => u.Phone == phone && u.DeletedAt == null, ct);

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await db.Users.AddAsync(user, ct);

    public async Task<IEnumerable<User>> GetAllAsync(int page, int pageSize, CancellationToken ct = default) =>
        await db.Users
            .Include(u => u.Profile)
            .Include(u => u.Security)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Where(u => u.DeletedAt == null)
            .OrderBy(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<int> CountAsync(CancellationToken ct = default) =>
        await db.Users.CountAsync(u => u.DeletedAt == null, ct);
}
