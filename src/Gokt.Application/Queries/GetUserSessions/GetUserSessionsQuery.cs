using Gokt.Application.DTOs;
using Gokt.Application.Interfaces;
using MediatR;

namespace Gokt.Application.Queries.GetUserSessions;

public record GetUserSessionsQuery(Guid UserId) : IRequest<IEnumerable<SessionDto>>;

public sealed class GetUserSessionsQueryHandler(ISessionRepository sessionRepository)
    : IRequestHandler<GetUserSessionsQuery, IEnumerable<SessionDto>>
{
    public async Task<IEnumerable<SessionDto>> Handle(GetUserSessionsQuery query, CancellationToken ct)
    {
        var sessions = await sessionRepository.GetActiveByUserIdAsync(query.UserId, ct);
        return sessions.Select(s => new SessionDto(
            s.Id,
            s.IpAddress,
            s.CreatedAt,
            s.LastUsedAt,
            s.ExpiresAt,
            IsCurrent: false
        ));
    }
}
