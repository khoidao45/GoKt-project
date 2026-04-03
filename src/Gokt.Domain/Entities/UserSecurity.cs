namespace Gokt.Domain.Entities;

public class UserSecurity
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public Guid UserId { get; private set; }
    public short FailedLoginAttempts { get; private set; }
    public DateTime? LockoutUntil { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public string? LastLoginIp { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public string? TwoFactorSecret { get; private set; }
    public string? EmailVerificationToken { get; private set; }
    public DateTime? EmailVerificationExpiry { get; private set; }
    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetExpiry { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public User User { get; private set; } = default!;

    private UserSecurity() { }

    public static UserSecurity Create(Guid userId) => new() { UserId = userId };

    public bool IsLockedOut() =>
        LockoutUntil.HasValue && LockoutUntil.Value > DateTime.UtcNow;

    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= MaxFailedAttempts)
            LockoutUntil = DateTime.UtcNow.Add(LockoutDuration);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordSuccessfulLogin(string ipAddress)
    {
        FailedLoginAttempts = 0;
        LockoutUntil = null;
        LastLoginAt = DateTime.UtcNow;
        LastLoginIp = ipAddress;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetEmailVerificationToken(string tokenHash)
    {
        EmailVerificationToken = tokenHash;
        EmailVerificationExpiry = DateTime.UtcNow.AddHours(24);
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsEmailVerificationTokenValid(string tokenHash) =>
        EmailVerificationToken == tokenHash &&
        EmailVerificationExpiry.HasValue &&
        EmailVerificationExpiry.Value > DateTime.UtcNow;

    public void ClearEmailVerificationToken()
    {
        EmailVerificationToken = null;
        EmailVerificationExpiry = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPasswordResetToken(string tokenHash)
    {
        PasswordResetToken = tokenHash;
        PasswordResetExpiry = DateTime.UtcNow.AddMinutes(30);
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsPasswordResetTokenValid(string tokenHash) =>
        PasswordResetToken == tokenHash &&
        PasswordResetExpiry.HasValue &&
        PasswordResetExpiry.Value > DateTime.UtcNow;

    public void ClearPasswordResetToken()
    {
        PasswordResetToken = null;
        PasswordResetExpiry = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void EnableTwoFactor(string secret)
    {
        TwoFactorSecret = secret;
        TwoFactorEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DisableTwoFactor()
    {
        TwoFactorSecret = null;
        TwoFactorEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
