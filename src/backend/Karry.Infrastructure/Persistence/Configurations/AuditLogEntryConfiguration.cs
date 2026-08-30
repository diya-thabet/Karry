using Karry.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karry.Infrastructure.Persistence.Configurations;

public sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("audit_log");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.Action).HasMaxLength(100).IsRequired();
        builder.Property(e => e.EntityType).HasMaxLength(64);
        builder.Property(e => e.EntityId).HasMaxLength(64);
        builder.Property(e => e.Before).HasColumnType("text");
        builder.Property(e => e.After).HasColumnType("text");
        builder.Property(e => e.IpAddress).HasMaxLength(45);
        builder.Property(e => e.DeviceId).HasMaxLength(128);
        builder.Property(e => e.Outcome).HasConversion<int>();
        builder.Property(e => e.OccurredAtUtc).IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.OccurredAtUtc });
        builder.HasIndex(e => e.EntityType);
    }
}