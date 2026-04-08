using Gokt.Domain.Enums;

namespace Gokt.Domain.Entities;

public class DriverWalletTransaction
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid DriverWalletId { get; private set; }
    public Guid DriverId { get; private set; }
    public WalletTransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string Description { get; private set; } = default!;
    public DateOnly RevenueDate { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public DriverWallet DriverWallet { get; private set; } = default!;
    public Driver Driver { get; private set; } = default!;

    private DriverWalletTransaction() { }

    public static DriverWalletTransaction Create(
        Guid driverWalletId,
        Guid driverId,
        WalletTransactionType type,
        decimal amount,
        DateOnly revenueDate,
        string description)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));

        return new DriverWalletTransaction
        {
            DriverWalletId = driverWalletId,
            DriverId = driverId,
            Type = type,
            Amount = amount,
            RevenueDate = revenueDate,
            Description = description
        };
    }
}
