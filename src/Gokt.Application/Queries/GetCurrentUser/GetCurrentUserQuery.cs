using Gokt.Application.DTOs;
using Gokt.Application.Interfaces;
using Gokt.Domain.Exceptions;
using MediatR;

namespace Gokt.Application.Queries.GetCurrentUser;

public record GetCurrentUserQuery(Guid UserId) : IRequest<UserDto>;

public sealed class GetCurrentUserQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    public async Task<UserDto> Handle(GetCurrentUserQuery query, CancellationToken ct)
    {
        var user = await userRepository.GetByIdWithRolesAsync(query.UserId, ct)
            ?? throw new NotFoundException("User", query.UserId);

        return UserDto.From(user);
    }
}
