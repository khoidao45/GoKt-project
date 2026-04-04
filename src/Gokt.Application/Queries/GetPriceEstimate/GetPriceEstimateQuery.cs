using Gokt.Application.DTOs;
using Gokt.Application.Interfaces;
using Gokt.Domain.Enums;
using MediatR;

namespace Gokt.Application.Queries.GetPriceEstimate;

public record GetPriceEstimateQuery(
    double PickupLatitude,
    double PickupLongitude,
    double DropoffLatitude,
    double DropoffLongitude,
    VehicleType VehicleType
) : IRequest<PriceEstimateDto>;

public sealed class GetPriceEstimateQueryHandler(IPricingService pricingService)
    : IRequestHandler<GetPriceEstimateQuery, PriceEstimateDto>
{
    public async Task<PriceEstimateDto> Handle(GetPriceEstimateQuery query, CancellationToken ct)
    {
        var (fare, distanceKm) = await pricingService.CalculateAsync(
            query.PickupLatitude, query.PickupLongitude,
            query.DropoffLatitude, query.DropoffLongitude,
            query.VehicleType, ct);

        return new PriceEstimateDto(query.VehicleType, fare, distanceKm);
    }
}
