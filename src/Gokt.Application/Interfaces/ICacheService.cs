namespace Gokt.Application.Interfaces;

public interface ICacheService
{
    Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken ct = default);
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
}
