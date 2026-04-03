namespace Gokt.Domain.Entities;

public class OAuthAccount
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string Provider { get; private set; } = default!;
    public string ProviderUserId { get; private set; } = default!;
    public string? ProviderEmail { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public User User { get; private set; } = default!;

    private OAuthAccount() { }

    public static OAuthAccount Create(Guid userId, string provider, string providerUserId, string? providerEmail = null) =>
        new()
        {
            UserId = userId,
            Provider = provider.ToUpperInvariant(),
            ProviderUserId = providerUserId,
            ProviderEmail = providerEmail
        };
}
