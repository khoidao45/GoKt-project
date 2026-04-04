using Gokt.Application.DTOs;
using Gokt.Application.Interfaces;
using Gokt.Domain.Enums;
using MediatR;

namespace Gokt.Application.Queries.GetNearbyDrivers;

public record GetNearbyDriversQuery(
    double Latitude,
    double Longitude,
    double RadiusKm = 5,
    VehicleType? VehicleType = null
) : IRequest<IEnumerable<DriverDto>>;

public sealed class GetNearbyDriversQueryHandler(
    IDriverRepository driverRepository,
    IUserRepository userRepository) : IRequestHandler<GetNearbyDriversQuery, IEnumerable<DriverDto>>
{
    public async Task<IEnumerable<DriverDto>> Handle(GetNearbyDriversQuery query, CancellationToken ct)
    {
        var drivers = await driverRepository.GetOnlineByVehicleTypeAsync(
            query.Latitude, query.Longitude, query.RadiusKm, query.VehicleType, ct);

        var result = new List<DriverDto>();
        foreach (var driver in drivers)
        {
            var user = await userRepository.GetByIdAsync(driver.UserId, ct);
            var fullName = user?.Profile != null
                ? $"{user.Profile.FirstName} {user.Profile.LastName}".Trim()
                : user?.Email ?? "Driver";
            result.Add(DriverDto.From(driver, fullName));
        }

        return result;
    }
}
