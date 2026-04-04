namespace Gokt.Application.Interfaces;

public interface IMatchingStrategy
{
    MatchingStrategyType StrategyType { get; }
    Task<MatchResult> ExecuteAsync(MatchingContext context, CancellationToken ct);
}

public enum MatchingStrategyType
{
    Auto,
    DriverCode
}

public record MatchingContext(
    Guid RideRequestId,
    double PickupLat,
    double PickupLng,
    string VehicleType,
    Guid CustomerId,
    string? DriverCode = null);

public record MatchResult(bool Success, Guid? AssignedDriverId = null, string? FailureReason = null);
