using Gokt.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gokt.Infrastructure.Persistence.Configurations;

public class UserSecurityConfiguration : IEntityTypeConfiguration<UserSecurity>
{
    public void Configure(EntityTypeBuilder<UserSecurity> builder)
    {
        builder.ToTable("UserSecurity");
        builder.HasKey(s => s.UserId);

        builder.Property(s => s.LastLoginIp).HasMaxLength(45);
        builder.Property(s => s.TwoFactorSecret).HasMaxLength(256);
        builder.Property(s => s.EmailVerificationToken).HasMaxLength(512);
        builder.Property(s => s.PasswordResetToken).HasMaxLength(512);
    }
}
