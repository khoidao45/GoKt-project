using Gokt.Application.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Gokt.Middleware;

/// <summary>
/// Checks whether the JTI (JWT ID) of the current request's access token has been blacklisted.
/// Tokens are blacklisted at logout time with a TTL equal to the access token lifetime.
///
/// Must be placed AFTER UseAuthentication (so HttpContext.User is populated)
/// and BEFORE UseAuthorization (so blacklisted tokens fail before reaching controllers).
/// </summary>
public class JwtBlacklistMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var jti = context.User.FindFirstValue(JwtRegisteredClaimNames.Jti);

            if (!string.IsNullOrEmpty(jti))
            {
                // Create a scope because ICacheService is registered as Scoped,
                // while middleware is a singleton — we must resolve it per-request.
                using var scope = scopeFactory.CreateScope();
                var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

                if (await cache.ExistsAsync($"blacklist:{jti}"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        code = "TOKEN_REVOKED",
                        message = "This access token has been revoked. Please log in again.",
                        timestamp = DateTime.UtcNow
                    });
                    return;
                }
            }
        }

        await next(context);
    }
}
