using Gokt.Application.Interfaces;
using Gokt.Domain.Entities;
using Gokt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gokt.Infrastructure.Repositories;

public class NotificationRepository(AppDbContext db) : INotificationRepository
{
    public async Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken ct = default) =>
        await db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

    public Task AddAsync(Notification notification, CancellationToken ct = default) =>
        db.Notifications.AddAsync(notification, ct).AsTask();

    public async Task MarkReadAsync(Guid userId, IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        var notifications = await db.Notifications
            .Where(n => n.UserId == userId && idList.Contains(n.Id) && !n.IsRead)
            .ToListAsync(ct);

        foreach (var n in notifications)
            n.MarkRead();
    }
}
