using Gokt.Application.Interfaces;
using StackExchange.Redis;

namespace Gokt.Infrastructure.Services;

public class RedisRateLimiter(IConnectionMultiplexer redis) : IRateLimiter
{
    public async Task<bool> IsAllowedAsync(string key, TimeSpan window, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        var count = await db.StringIncrementAsync(key);
        if (count == 1)
            await db.KeyExpireAsync(key, window);

        return count == 1;
    }
}
