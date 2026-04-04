using Gokt.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gokt.Infrastructure.Persistence.Configurations;

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.RefreshTokenHash).IsRequired().HasMaxLength(512);
        builder.Property(s => s.IpAddress).HasMaxLength(45);
        builder.Property(s => s.UserAgent).HasMaxLength(512);
        builder.Property(s => s.RevokedAt);
        builder.Property(s => s.ReplacedByTokenHash).HasMaxLength(512);

        builder.HasIndex(s => s.RefreshTokenHash).IsUnique();
        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.ExpiresAt);
    }
}
