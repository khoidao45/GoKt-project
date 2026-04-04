namespace Gokt.Application.Interfaces;

public interface IMatchingService
{
    /// <summary>
    /// Starts the matching process for a ride request.
    /// This method returns immediately; matching runs as a fire-and-forget background task.
    /// </summary>
    Task StartMatchingAsync(Guid rideRequestId, CancellationToken ct = default);
}
