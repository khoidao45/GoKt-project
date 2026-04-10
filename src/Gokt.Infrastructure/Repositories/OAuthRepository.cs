using Gokt.Application.Interfaces;
using Gokt.Domain.Entities;
using Gokt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gokt.Infrastructure.Repositories;

public class OAuthRepository(AppDbContext db) : IOAuthRepository
{
    public Task<OAuthAccount?> GetByProviderAsync(string provider, string providerUserId, CancellationToken ct = default) =>
        db.OAuthAccounts
            .Include(o => o.User).ThenInclude(u => u.Profile)
            .Include(o => o.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(
                o => o.Provider == provider.ToUpperInvariant() && o.ProviderUserId == providerUserId,
                ct);

    public async Task AddAsync(OAuthAccount account, CancellationToken ct = default) =>
        await db.OAuthAccounts.AddAsync(account, ct);

    public Task RemoveAsync(OAuthAccount account, CancellationToken ct = default)
    {
        db.OAuthAccounts.Remove(account);
        return Task.CompletedTask;
    }
}
