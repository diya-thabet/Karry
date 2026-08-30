using Karry.Domain.Maintenance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karry.Infrastructure.Persistence.Configurations;

public sealed class WearPartConfiguration : IEntityTypeConfiguration<WearPart>
{
    public void Configure(EntityTypeBuilder<WearPart> builder)
    {
        builder.ToTable("wear_parts");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.TenantId).IsRequired();
        builder.Property(w => w.MachineId).IsRequired();
        builder.Property(w => w.Name).HasMaxLength(150).IsRequired();
        builder.Property(w => w.Category).HasMaxLength(100);
        builder.Property(w => w.ActiveMeter).HasConversion<int>().IsRequired();

        builder.Property(w => w.RatingHours).HasDefaultValue(0);
        builder.Property(w => w.RatingKilometers).HasDefaultValue(0);
        builder.Property(w => w.RatingMetricTons).HasDefaultValue(0);
        builder.Property(w => w.BondAbrasionIndex).HasDefaultValue(1.0);
        builder.Property(w => w.AccumulatedHours).HasDefaultValue(0);
        builder.Property(w => w.AccumulatedKilometers).HasDefaultValue(0);
        builder.Property(w => w.ProcessedMetricTons).HasDefaultValue(0);

        builder.HasIndex(w => new { w.TenantId, w.MachineId });
    }
}