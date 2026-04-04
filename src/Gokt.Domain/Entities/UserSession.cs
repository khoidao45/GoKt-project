namespace Gokt.Domain.Entities;

public class UserSession
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string RefreshTokenHash { get; private set; } = default!;
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }  // forensic chain: old → new token
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime LastUsedAt { get; private set; } = DateTime.UtcNow;

    public User User { get; private set; } = default!;

    private UserSession() { }

    public static UserSession Create(
        Guid userId,
        string refreshTokenHash,
        DateTime expiresAt,
        string? ipAddress = null,
        string? userAgent = null) =>
        new()
        {
            UserId = userId,
            RefreshTokenHash = refreshTokenHash,
            ExpiresAt = expiresAt,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

    public bool IsValid() => !IsRevoked && ExpiresAt > DateTime.UtcNow;

    public void Revoke()
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
    }

    public void RotateToken(string newTokenHash, DateTime newExpiry)
    {
        ReplacedByTokenHash = newTokenHash;   // link old session → new token hash (forensic chain)
        RefreshTokenHash = newTokenHash;
        ExpiresAt = newExpiry;
        LastUsedAt = DateTime.UtcNow;
    }
}
