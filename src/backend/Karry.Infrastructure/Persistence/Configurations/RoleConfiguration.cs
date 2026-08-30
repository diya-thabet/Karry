using Karry.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karry.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.TenantId).IsRequired();
        builder.Property(r => r.Code).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Name).HasMaxLength(120).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(512);

        builder.OwnsMany(r => r.Permissions, permissions =>
        {
            permissions.ToTable("role_permissions");
            permissions.WithOwner().HasForeignKey("RoleId");
            permissions.HasKey("RoleId", "PermissionId");
            permissions.Property(rp => rp.PermissionId);
            permissions.Property(rp => rp.Resource).HasMaxLength(64).IsRequired();
            permissions.Property(rp => rp.Action).HasConversion<int>();
        });

        builder.HasIndex(r => new { r.TenantId, r.Code }).IsUnique();
        builder.HasIndex(r => r.TenantId);
    }
}