using Gokt.Application.Commands.Rides.CreateRideRequest;
using Gokt.Application.Interfaces;
using Gokt.Domain.Exceptions;
using MediatR;

namespace Gokt.Application.Common.Behaviors;

/// <summary>
/// Allows at most 1 CreateRideRequestCommand per user per 5 seconds.
/// </summary>
public sealed class RateLimitBehavior<TRequest, TResponse>(IRateLimiter rateLimiter)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(5);

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is not CreateRideRequestCommand cmd)
            return await next();

        var key = $"gokt:ratelimit:ride:{cmd.CustomerId}";
        var allowed = await rateLimiter.IsAllowedAsync(key, Window, ct);

        if (!allowed)
            throw new ConflictException("Too many requests. Please wait a few seconds before requesting another ride.");

        return await next();
    }
}
