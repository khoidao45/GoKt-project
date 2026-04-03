using Gokt.Application.Interfaces;
using Gokt.Infrastructure.Persistence;

namespace Gokt.Infrastructure.Repositories;

public class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
