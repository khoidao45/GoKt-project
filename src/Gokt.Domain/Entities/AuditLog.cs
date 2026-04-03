namespace Gokt.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid? UserId { get; private set; }       // nullable — pre-auth events have no user
    public string Action { get; private set; } = default!;
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? Details { get; private set; }    // JSON-serialized metadata
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private AuditLog() { }

    public static AuditLog Create(
        string action,
        Guid? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? details = null) =>
        new()
        {
            Action = action,
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Details = details
        };
}
