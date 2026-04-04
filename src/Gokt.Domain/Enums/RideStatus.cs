namespace Gokt.Domain.Enums;

public enum RideStatus
{
    Pending   = 1,
    Accepted  = 2,
    Cancelled = 3,
    Expired   = 4,
    Searching = 5   // matching engine is actively running
}
