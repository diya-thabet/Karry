using Karry.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karry.Infrastructure.Persistence.Configurations;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Resource).HasMaxLength(64).IsRequired();
        builder.Property(p => p.Action).HasConversion<int>();
        builder.Property(p => p.Description).HasMaxLength(256);

        builder.HasIndex(p => new { p.Resource, p.Action }).IsUnique();
    }
}