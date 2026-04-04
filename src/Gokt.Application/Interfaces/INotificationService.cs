using Gokt.Domain.Enums;

namespace Gokt.Application.Interfaces;

public interface INotificationService
{
    Task SendAsync(
        Guid userId,
        string title,
        string body,
        NotificationType type,
        object? data = null,
        CancellationToken ct = default);
}
