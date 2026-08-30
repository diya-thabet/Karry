using Karry.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Karry.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.TenantId);

        builder.Property(u => u.Email)
            .HasConversion<string>(new ValueConverter<EmailAddress, string>(
                email => email.Value,
                value => EmailAddress.Create(value)))
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(u => u.Name).HasMaxLength(150).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();

        builder.Property(u => u.IsActive).HasDefaultValue(true);
        builder.Property(u => u.IsPlatformAdmin).HasDefaultValue(false);
        builder.Property(u => u.FailedLoginCount).HasDefaultValue(0);
        builder.Property(u => u.TwoFactorEnabled).HasDefaultValue(false);
        builder.Property(u => u.TotpSecret).HasMaxLength(64).HasDefaultValue("");

        builder.Property(u => u.RoleId);

        builder.Property(u => u.DeviceIds)
            .HasColumnType("text[]")
            .HasConversion(
                v => v.ToArray(),
                v => v.ToList());

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.HasIndex(u => new { u.TenantId, u.RoleId });
        builder.HasIndex(u => u.TenantId);
    }
}