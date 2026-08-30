using Karry.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karry.Infrastructure.Persistence.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).HasMaxLength(150).IsRequired();
        builder.Property(t => t.Country).HasMaxLength(80).IsRequired();
        builder.Property(t => t.Currency).HasMaxLength(3).IsRequired();
        builder.Property(t => t.Timezone).HasMaxLength(64).IsRequired();
        builder.Property(t => t.Locale).HasMaxLength(5).IsRequired();

        builder.HasIndex(t => t.Name).IsUnique();
    }
}