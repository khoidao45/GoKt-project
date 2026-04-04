using System.Text.Json;
using Gokt.Application.Interfaces;
using Gokt.Domain.Entities;
using Gokt.Domain.Enums;
using Gokt.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace Gokt.Infrastructure.Services;

public class NotificationService(
    AppDbContext db,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task SendAsync(
        Guid userId,
        string title,
        string body,
        NotificationType type,
        object? data = null,
        CancellationToken ct = default)
    {
        try
        {
            var jsonData = data is not null
                ? JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
                : null;

            var notification = Notification.Create(userId, title, body, type, jsonData);
            await db.Notifications.AddAsync(notification, ct);
            await db.SaveChangesAsync(ct);

            // TODO: integrate FCM/APNs for push notifications
            logger.LogInformation("Notification sent to user {UserId}: [{Type}] {Title}", userId, type, title);
        }
        catch (Exception ex)
        {
            // Notification failures must never crash business logic
            logger.LogError(ex, "Failed to send notification to user {UserId}", userId);
        }
    }
}
