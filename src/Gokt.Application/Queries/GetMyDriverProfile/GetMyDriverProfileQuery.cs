using Gokt.Application.DTOs;
using Gokt.Application.Interfaces;
using MediatR;

namespace Gokt.Application.Queries.GetMyDriverProfile;

public record GetMyDriverProfileQuery(Guid UserId) : IRequest<DriverDto?>;

public sealed class GetMyDriverProfileQueryHandler(
    IDriverRepository driverRepository,
    IUserRepository userRepository)
    : IRequestHandler<GetMyDriverProfileQuery, DriverDto?>
{
    public async Task<DriverDto?> Handle(GetMyDriverProfileQuery query, CancellationToken ct)
    {
        var driver = await driverRepository.GetByUserIdAsync(query.UserId, ct);
        if (driver is null) return null;

        var user = await userRepository.GetByIdAsync(query.UserId, ct);
        var fullName = user?.Profile != null
            ? $"{user.Profile.FirstName} {user.Profile.LastName}".Trim()
            : query.UserId.ToString();

        return DriverDto.From(driver, fullName, user?.Profile?.AvatarUrl);
    }
}
