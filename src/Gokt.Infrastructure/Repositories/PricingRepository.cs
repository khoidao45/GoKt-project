using Gokt.Application.Interfaces;
using Gokt.Domain.Entities;
using Gokt.Domain.Enums;
using Gokt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gokt.Infrastructure.Repositories;

public class PricingRepository(AppDbContext db) : IPricingRepository
{
    public Task<PricingRule?> GetByVehicleTypeAsync(VehicleType vehicleType, CancellationToken ct = default) =>
        db.PricingRules.FirstOrDefaultAsync(p => p.VehicleType == vehicleType && p.IsActive, ct);

    public async Task<IEnumerable<PricingRule>> GetAllActiveAsync(CancellationToken ct = default) =>
        await db.PricingRules.Where(p => p.IsActive).ToListAsync(ct);
}
