using Gokt.Domain.Entities;
using Gokt.Domain.Enums;

namespace Gokt.Application.Interfaces;

public interface IPricingRepository
{
    Task<PricingRule?> GetByVehicleTypeAsync(VehicleType vehicleType, CancellationToken ct = default);
    Task<IEnumerable<PricingRule>> GetAllActiveAsync(CancellationToken ct = default);
}
