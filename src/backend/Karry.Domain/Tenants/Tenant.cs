using Karry.Domain.Common;

namespace Karry.Domain.Tenants;

public sealed class Tenant : BaseEntity, IAuditableEntity
{
    public string Name { get; private set; } = default!;

    public string Country { get; private set; } = default!;

    public string Currency { get; private set; } = "USD";

    public string Timezone { get; private set; } = "UTC";

    public string Locale { get; private set; } = "en";

    public Guid CreatedBy { get; private set; }

    public Guid? ModifiedBy { get; private set; }

    private Tenant()
    {
    }

    public static Tenant Create(string name, string country, string currency, string timezone, string locale, Guid createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tenant name is required.", nameof(name));
        }

        return new Tenant
        {
            Name = name.Trim(),
            Country = country.Trim(),
            Currency = currency.Trim().ToUpperInvariant(),
            Timezone = timezone,
            Locale = locale,
            CreatedBy = createdBy,
        };
    }

    public void UpdateProfile(string name, string country, string currency, string timezone, string locale, Guid modifiedBy)
    {
        Name = name.Trim();
        Country = country.Trim();
        Currency = currency.Trim().ToUpperInvariant();
        Timezone = timezone;
        Locale = locale;
        ModifiedBy = modifiedBy;
        MarkUpdated();
    }
}