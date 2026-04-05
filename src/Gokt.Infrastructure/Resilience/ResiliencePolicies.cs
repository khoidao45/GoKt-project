using Npgsql;
using Polly;
using Polly.Retry;
using StackExchange.Redis;

namespace Gokt.Infrastructure.Resilience;

public static class ResiliencePolicies
{
    public static AsyncRetryPolicy DbRetry { get; } =
        Policy
            .Handle<NpgsqlException>(ex => ex.IsTransient)
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

    public static AsyncRetryPolicy RedisRetry { get; } =
        Policy
            .Handle<RedisConnectionException>()
            .Or<RedisTimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: _ => TimeSpan.FromMilliseconds(200));
}
