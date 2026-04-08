using Gokt.Domain.Enums;
using Gokt.Domain.Events;

namespace Gokt.Domain.Entities;

public class User
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Email { get; private set; } = default!;
    public string? Phone { get; private set; }
    public string? PasswordHash { get; private set; }
    public UserStatus Status { get; private set; } = UserStatus.PendingVerification;
    public bool EmailVerified { get; private set; }
    public bool PhoneVerified { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; private set; }

    // Navigation properties
    public UserProfile Profile { get; private set; } = default!;
    public UserSecurity Security { get; private set; } = default!;
    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();
    public ICollection<UserSession> Sessions { get; private set; } = new List<UserSession>();
    public ICollection<OAuthAccount> OAuthAccounts { get; private set; } = new List<OAuthAccount>();

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private User() { } // EF Core

    public static User Create(string email, string? passwordHash, string? firstName, string? lastName, string? phone = null)
    {
        var user = new User
        {
            Email = email.ToLowerInvariant().Trim(),
            PasswordHash = passwordHash,
            Phone = phone?.Trim()
        };

        user.Profile = UserProfile.Create(user.Id, firstName, lastName);
        user.Security = UserSecurity.Create(user.Id);
        user._domainEvents.Add(new UserRegisteredEvent(user.Id, user.Email));

        return user;
    }

    public static User CreateOAuth(string email, string? firstName, string? lastName, string? avatarUrl = null)
    {
        var user = new User
        {
            Email = email.ToLowerInvariant().Trim(),
            EmailVerified = true,
            Status = UserStatus.Active,
        };

        user.Profile = UserProfile.Create(user.Id, firstName, lastName, avatarUrl);
        user.Security = UserSecurity.Create(user.Id);
        user._domainEvents.Add(new UserRegisteredEvent(user.Id, user.Email));

        return user;
    }

    public void VerifyEmail()
    {
        EmailVerified = true;
        if (Status == UserStatus.PendingVerification)
            Status = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        Status = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Suspend()
    {
        Status = UserStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        Status = UserStatus.Deleted;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        // Anonymize PII on deletion
        Email = $"deleted_{Id}@deleted.invalid";
        Phone = null;
    }

    public void SetPasswordHash(string hash)
    {
        PasswordHash = hash;
        UpdatedAt = DateTime.UtcNow;
    }

    public IEnumerable<string> GetRoleNames() =>
        UserRoles.Select(ur => ur.Role?.Name ?? string.Empty).Where(n => !string.IsNullOrEmpty(n));

    public bool IsActive() => Status == UserStatus.Active;

    public void ClearDomainEvents() => _domainEvents.Clear();
}
