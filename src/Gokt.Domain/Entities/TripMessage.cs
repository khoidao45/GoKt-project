namespace Gokt.Domain.Entities;

public class TripMessage
{
    public Guid Id { get; private set; }
    public Guid TripId { get; private set; }
    public Guid SenderId { get; private set; }
    public string SenderRole { get; private set; } = default!; // "Driver" | "Rider"
    public string Body { get; private set; } = default!;
    public DateTime SentAt { get; private set; }

    private TripMessage() { }

    public static TripMessage Create(Guid tripId, Guid senderId, string senderRole, string body) =>
        new()
        {
            Id = Guid.NewGuid(),
            TripId = tripId,
            SenderId = senderId,
            SenderRole = senderRole,
            Body = body,
            SentAt = DateTime.UtcNow,
        };
}
