using Gokt.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gokt.Infrastructure.Persistence.Configurations;

public class OAuthAccountConfiguration : IEntityTypeConfiguration<OAuthAccount>
{
    public void Configure(EntityTypeBuilder<OAuthAccount> builder)
    {
        builder.ToTable("OAuthAccounts");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Provider).IsRequired().HasMaxLength(30);
        builder.Property(o => o.ProviderUserId).IsRequired().HasMaxLength(255);
        builder.Property(o => o.ProviderEmail).HasMaxLength(255);

        builder.HasIndex(o => new { o.Provider, o.ProviderUserId }).IsUnique();
        builder.HasIndex(o => o.UserId);
    }
}
