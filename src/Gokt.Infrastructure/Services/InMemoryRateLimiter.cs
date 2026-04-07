using System.Collections.Concurrent;
using Gokt.Application.Interfaces;

namespace Gokt.Infrastructure.Services;

public class InMemoryRateLimiter : IRateLimiter
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _windows = new();

    public Task<bool> IsAllowedAsync(string key, TimeSpan window, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var until = _windows.GetOrAdd(key, _ => now.Add(window));

        if (until <= now)
        {
            _windows[key] = now.Add(window);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}
