using Gokt.Application.Interfaces;
using MediatR;

namespace Gokt.Application.Commands.Notifications.MarkNotificationsRead;

public record MarkNotificationsReadCommand(Guid UserId, List<Guid> NotificationIds) : IRequest;

public sealed class MarkNotificationsReadCommandHandler(
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<MarkNotificationsReadCommand>
{
    public async Task Handle(MarkNotificationsReadCommand cmd, CancellationToken ct)
    {
        await notificationRepository.MarkReadAsync(cmd.UserId, cmd.NotificationIds, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
