using Gokt.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gokt.Infrastructure.Persistence.Configurations;

public class DriverWalletTransactionConfiguration : IEntityTypeConfiguration<DriverWalletTransaction>
{
    public void Configure(EntityTypeBuilder<DriverWalletTransaction> builder)
    {
        builder.ToTable("DriverWalletTransactions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.RevenueDate).HasColumnType("date");
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.HasOne(x => x.DriverWallet)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.DriverWalletId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.DriverId);
        builder.HasIndex(x => x.RevenueDate);
        builder.HasIndex(x => new { x.DriverId, x.RevenueDate });
    }
}
