using Gokt.Domain.Entities;

namespace Gokt.Application.Interfaces;

public interface IOAuthRepository
{
    Task<OAuthAccount?> GetByProviderAsync(string provider, string providerUserId, CancellationToken ct = default);
    Task AddAsync(OAuthAccount account, CancellationToken ct = default);
    Task RemoveAsync(OAuthAccount account, CancellationToken ct = default);
}
