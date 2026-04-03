using Gokt.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gokt.Infrastructure.Persistence.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.FirstName).HasMaxLength(100);
        builder.Property(p => p.LastName).HasMaxLength(100);
        builder.Property(p => p.AvatarUrl).HasMaxLength(2048);
        builder.Property(p => p.Gender).HasMaxLength(20);
        builder.Property(p => p.Address).HasMaxLength(500);

        builder.HasIndex(p => p.UserId).IsUnique();
        builder.Ignore(p => p.FullName);
    }
}
