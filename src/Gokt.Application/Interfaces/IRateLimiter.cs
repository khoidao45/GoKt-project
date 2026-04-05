namespace Gokt.Application.Interfaces;

public interface IRateLimiter
{
    /// <summary>Returns false if the key has been seen within <paramref name="window"/> already.</summary>
    Task<bool> IsAllowedAsync(string key, TimeSpan window, CancellationToken ct = default);
}
