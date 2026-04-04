using Gokt.Application.DTOs;
using Gokt.Application.Interfaces;
using MediatR;

namespace Gokt.Application.Queries.GetNotifications;

public record GetNotificationsQuery(Guid UserId, int Page = 1, int PageSize = 20) : IRequest<IEnumerable<NotificationDto>>;

public sealed class GetNotificationsQueryHandler(INotificationRepository notificationRepository)
    : IRequestHandler<GetNotificationsQuery, IEnumerable<NotificationDto>>
{
    public async Task<IEnumerable<NotificationDto>> Handle(GetNotificationsQuery query, CancellationToken ct)
    {
        var notifications = await notificationRepository.GetByUserIdAsync(query.UserId, query.Page, query.PageSize, ct);
        return notifications.Select(NotificationDto.From);
    }
}
