using Karry.Domain.Units;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karry.Infrastructure.Persistence.Configurations;

public sealed class TenantUnitPreferenceConfiguration : IEntityTypeConfiguration<TenantUnitPreference>
{
    public void Configure(EntityTypeBuilder<TenantUnitPreference> builder)
    {
        builder.ToTable("tenant_unit_preferences");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.TenantId).IsRequired();
        builder.Property(p => p.DefaultMassUnit).HasConversion<int>();
        builder.Property(p => p.DefaultVolumeUnit).HasConversion<int>();

        builder.HasIndex(p => p.TenantId).IsUnique();
    }
}

public sealed class UserUnitPreferenceConfiguration : IEntityTypeConfiguration<UserUnitPreference>
{
    public void Configure(EntityTypeBuilder<UserUnitPreference> builder)
    {
        builder.ToTable("user_unit_preferences");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.TenantId).IsRequired();
        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.MassUnit).HasConversion<int?>();
        builder.Property(p => p.VolumeUnit).HasConversion<int>();

        builder.HasIndex(p => new { p.TenantId, p.UserId }).IsUnique();
    }
}