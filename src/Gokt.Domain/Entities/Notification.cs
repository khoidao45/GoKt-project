using Gokt.Domain.Enums;

namespace Gokt.Domain.Entities;

public class Notification
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public NotificationType Type { get; private set; }
    public string? Data { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; private set; }

    public User User { get; private set; } = default!;

    private Notification() { }

    public static Notification Create(
        Guid userId, string title, string body, NotificationType type, string? data = null) =>
        new()
        {
            UserId = userId,
            Title = title,
            Body = body,
            Type = type,
            Data = data
        };

    public void MarkRead()
    {
        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }
}
