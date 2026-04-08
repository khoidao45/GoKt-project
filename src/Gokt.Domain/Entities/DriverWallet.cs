namespace Gokt.Domain.Entities;

public class DriverWallet
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid DriverId { get; private set; }
    public decimal AvailableBalance { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public Driver Driver { get; private set; } = default!;
    public ICollection<DriverWalletTransaction> Transactions { get; private set; } = new List<DriverWalletTransaction>();

    private DriverWallet() { }

    public static DriverWallet Create(Guid driverId)
    {
        return new DriverWallet
        {
            DriverId = driverId,
            AvailableBalance = 0m
        };
    }

    public void Credit(decimal amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        AvailableBalance += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Debit(decimal amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        AvailableBalance -= amount;
        UpdatedAt = DateTime.UtcNow;
    }
}
