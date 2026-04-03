using System.Text.Json;
using Gokt.Application.Interfaces;
using Gokt.Domain.Entities;
using Gokt.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace Gokt.Infrastructure.Services;

public class AuditService(AppDbContext db, ILogger<AuditService> logger) : IAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task LogAsync(
        string action,
        Guid? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        object? details = null,
        CancellationToken ct = default)
    {
        try
        {
            var detailsJson = details is null
                ? null
                : JsonSerializer.Serialize(details, JsonOptions);

            var entry = AuditLog.Create(action, userId, ipAddress, userAgent, detailsJson);
            await db.AuditLogs.AddAsync(entry, ct);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Audit logging must never crash the calling business flow
            logger.LogWarning(ex, "Failed to write audit log for action {Action} by user {UserId}", action, userId);
        }
    }
}
