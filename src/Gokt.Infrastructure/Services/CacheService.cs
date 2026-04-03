using System.Text.Json;
using Gokt.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Gokt.Infrastructure.Services;

public class CacheService(IDistributedCache cache, ILogger<CacheService> logger) : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            await cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry
            }, ct);
        }
        catch (Exception ex)
        {
            // Cache failures should never break the main flow
            logger.LogWarning(ex, "Cache SET failed for key {Key}", key);
        }
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var json = await cache.GetStringAsync(key, ct);
            return json is null ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache GET failed for key {Key}", key);
            return default;
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try { await cache.RemoveAsync(key, ct); }
        catch (Exception ex) { logger.LogWarning(ex, "Cache REMOVE failed for key {Key}", key); }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try { return await cache.GetAsync(key, ct) is not null; }
        catch { return false; }
    }
}
