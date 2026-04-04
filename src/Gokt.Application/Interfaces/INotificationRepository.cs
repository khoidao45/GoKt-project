using Gokt.Domain.Entities;

namespace Gokt.Application.Interfaces;

public interface INotificationRepository
{
    Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Notification notification, CancellationToken ct = default);
    Task MarkReadAsync(Guid userId, IEnumerable<Guid> ids, CancellationToken ct = default);
}
