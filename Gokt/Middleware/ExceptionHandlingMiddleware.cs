using System.Net;
using System.Text.Json;
using Gokt.Domain.Exceptions;

namespace Gokt.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, code, message, errors) = exception switch
        {
            ValidationException ve => (
                HttpStatusCode.BadRequest,
                ve.Code,
                ve.Message,
                (object?)ve.Errors),

            ConflictException ce => (HttpStatusCode.Conflict, ce.Code, ce.Message, null),
            UnauthorizedException ue => (HttpStatusCode.Unauthorized, ue.Code, ue.Message, null),
            ForbiddenException fe => (HttpStatusCode.Forbidden, fe.Code, fe.Message, null),
            NotFoundException nfe => (HttpStatusCode.NotFound, nfe.Code, nfe.Message, null),
            TooManyRequestsException te => (HttpStatusCode.TooManyRequests, te.Code, te.Message, null),
            DomainException de => (HttpStatusCode.BadRequest, de.Code, de.Message, null),

            _ => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR",
                  "An unexpected error occurred.", null)
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            logger.LogError(exception, "Unhandled exception");

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            code,
            message,
            errors,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
