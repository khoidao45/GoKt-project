using Gokt.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gokt.Infrastructure.Persistence.Configurations;

public class DriverWalletConfiguration : IEntityTypeConfiguration<DriverWallet>
{
    public void Configure(EntityTypeBuilder<DriverWallet> builder)
    {
        builder.ToTable("DriverWallets");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AvailableBalance).HasPrecision(18, 2);

        builder.HasOne(x => x.Driver)
            .WithOne()
            .HasForeignKey<DriverWallet>(x => x.DriverId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.DriverId).IsUnique();
    }
}
