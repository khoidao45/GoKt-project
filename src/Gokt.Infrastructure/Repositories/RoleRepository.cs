using Gokt.Application.Interfaces;
using Gokt.Domain.Entities;
using Gokt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gokt.Infrastructure.Repositories;

public class RoleRepository(AppDbContext db) : IRoleRepository
{
    public async Task<Role?> GetByNameAsync(string name, CancellationToken ct = default) =>
        await db.Roles.FirstOrDefaultAsync(r => r.Name == name.ToUpperInvariant(), ct);

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Roles.FindAsync([id], ct);

    public async Task<IEnumerable<Role>> GetAllAsync(CancellationToken ct = default) =>
        await db.Roles.ToListAsync(ct);
}
