using System.Security.Claims;

namespace Gokt.Middleware;

/// <summary>
/// Blocks authenticated-but-unverified users from accessing protected resources.
/// Allows only selected auth endpoints so users can verify/resend/logout.
/// </summary>
public class EmailVerificationGuardMiddleware(RequestDelegate next)
{
    private static readonly string[] AllowedAuthPaths =
    [
        "/api/v1/auth/verify-email",
        "/api/v1/auth/verify-email-token",
        "/api/v1/auth/resend-verification",
        "/api/v1/auth/logout",
        "/api/v1/auth/logout-all"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        if (AllowedAuthPaths.Any(path.StartsWith))
        {
            await next(context);
            return;
        }

        var emailVerified = context.User.FindFirstValue("email_verified");
        if (!string.Equals(emailVerified, "true", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                code = "EMAIL_NOT_VERIFIED",
                message = "Vui lòng xác thực email trước khi truy cập hệ thống.",
                timestamp = DateTime.UtcNow
            });
            return;
        }

        await next(context);
    }
}
