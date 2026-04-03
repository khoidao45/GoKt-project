namespace Gokt.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(
        string action,
        Guid? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        object? details = null,
        CancellationToken ct = default);
}
