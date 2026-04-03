namespace Gokt.Domain.Entities;

public class UserProfile
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? AvatarUrl { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public string? Gender { get; private set; }
    public string? Address { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public User User { get; private set; } = default!;

    private UserProfile() { }

    public static UserProfile Create(Guid userId, string? firstName, string? lastName) =>
        new() { UserId = userId, FirstName = firstName, LastName = lastName };

    public void Update(string? firstName, string? lastName, string? avatarUrl,
        DateOnly? dateOfBirth, string? gender, string? address)
    {
        FirstName = firstName;
        LastName = lastName;
        AvatarUrl = avatarUrl;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        Address = address;
        UpdatedAt = DateTime.UtcNow;
    }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
