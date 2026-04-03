namespace Gokt.Domain.Entities;

public class Role
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

    private Role() { }

    public static Role Create(string name, string? description = null, bool isSystem = false) =>
        new() { Name = name, Description = description, IsSystem = isSystem };

    // Used only for EF Core HasData seeding (requires deterministic Id)
    public static Role CreateSeed(Guid id, string name, string? description, bool isSystem) =>
        new() { Id = id, Name = name, Description = description, IsSystem = isSystem };
}
