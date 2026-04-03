using Gokt.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gokt.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(50);
        builder.Property(r => r.Description).HasMaxLength(255);
        builder.HasIndex(r => r.Name).IsUnique();

        // Seed data
        builder.HasData(
            Role.CreateSeed(new Guid("a1a1a1a1-0000-0000-0000-000000000001"), "RIDER",   "Can book trips",                 true),
            Role.CreateSeed(new Guid("a1a1a1a1-0000-0000-0000-000000000002"), "DRIVER",  "Can accept and complete trips",  true),
            Role.CreateSeed(new Guid("a1a1a1a1-0000-0000-0000-000000000003"), "ADMIN",   "Platform administrator",         true),
            Role.CreateSeed(new Guid("a1a1a1a1-0000-0000-0000-000000000004"), "SUPPORT", "Customer support agent",         true)
        );
    }
}
