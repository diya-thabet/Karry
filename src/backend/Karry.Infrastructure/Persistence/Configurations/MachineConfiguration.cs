using Karry.Domain.Equipment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karry.Infrastructure.Persistence.Configurations;

public sealed class MachineConfiguration : IEntityTypeConfiguration<Machine>
{
    public void Configure(EntityTypeBuilder<Machine> builder)
    {
        builder.ToTable("machines");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.TenantId).IsRequired();
        builder.Property(m => m.SiteId).IsRequired();
        builder.Property(m => m.Name).HasMaxLength(120).IsRequired();
        builder.Property(m => m.Model).HasMaxLength(120);
        builder.Property(m => m.SerialNumber).HasMaxLength(120);
        builder.Property(m => m.Type).HasConversion<int>().IsRequired();

        builder.Property(m => m.AccumulatedHours).HasDefaultValue(0);
        builder.Property(m => m.AccumulatedKilometers).HasDefaultValue(0);

        builder.HasIndex(m => new { m.TenantId, m.SiteId });
        builder.HasIndex(m => m.SerialNumber).IsUnique();
    }
}