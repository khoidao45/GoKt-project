namespace Gokt.Domain.Events;

public sealed record UserLoggedInEvent(Guid UserId, string? IpAddress, string? DeviceType) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string EventType => "user.logged_in";
}
