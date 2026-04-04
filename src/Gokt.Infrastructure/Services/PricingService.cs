using Gokt.Application.Interfaces;
using Gokt.Domain.Enums;
using Gokt.Domain.Exceptions;

namespace Gokt.Infrastructure.Services;

public class PricingService(IPricingRepository pricingRepository) : IPricingService
{
    // Average speed assumption for time estimate: 30 km/h
    private const double AverageSpeedKmh = 30.0;

    public async Task<(decimal Fare, decimal DistanceKm)> CalculateAsync(
        double fromLat, double fromLng,
        double toLat, double toLng,
        VehicleType vehicleType,
        CancellationToken ct = default)
    {
        var rule = await pricingRepository.GetByVehicleTypeAsync(vehicleType, ct)
            ?? throw new DomainException("PRICING_NOT_FOUND", $"No pricing rule found for {vehicleType}.");

        var distanceKm = (decimal)HaversineKm(fromLat, fromLng, toLat, toLng);
        var estimatedMinutes = (decimal)(distanceKm / (decimal)AverageSpeedKmh * 60m);

        var fare = rule.BaseFare
                 + distanceKm * rule.PerKmRate
                 + estimatedMinutes * rule.PerMinuteRate;

        fare = Math.Max(fare, rule.MinimumFare) * rule.SurgeMultiplier;
        fare = Math.Round(fare, 2);

        return (fare, Math.Round(distanceKm, 2));
    }

    private static double HaversineKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371;
        var dLat = ToRad(lat2 - lat1);
        var dLng = ToRad(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;
}
