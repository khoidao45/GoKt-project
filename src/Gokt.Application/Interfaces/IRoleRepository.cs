using Gokt.Domain.Entities;

namespace Gokt.Application.Interfaces;

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Role>> GetAllAsync(CancellationToken ct = default);
}
