using Gokt.Domain.Enums;

namespace Gokt.Application.Interfaces;

public interface IPricingService
{
    Task<(decimal Fare, decimal DistanceKm)> CalculateAsync(
        double fromLat, double fromLng,
        double toLat, double toLng,
        VehicleType vehicleType,
        CancellationToken ct = default);
}
