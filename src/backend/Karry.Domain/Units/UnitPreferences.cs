using Karry.Domain.Common;

namespace Karry.Domain.Units;

/// <summary>
/// Per-tenant default units used by the dynamic unit toggle when a user has no override.
/// </summary>
public sealed class TenantUnitPreference : BaseEntity, ITenantScoped
{
    public Guid TenantId { get; private set; }

    public MassUnit DefaultMassUnit { get; private set; } = MassUnit.MetricTon;

    public VolumeUnit DefaultVolumeUnit { get; private set; } = VolumeUnit.CubicMeter;

    private TenantUnitPreference()
    {
    }

    public static TenantUnitPreference Create(Guid tenantId, MassUnit massUnit, VolumeUnit volumeUnit)
        => new() { TenantId = tenantId, DefaultMassUnit = massUnit, DefaultVolumeUnit = volumeUnit };

    public void SetDefaults(MassUnit massUnit, VolumeUnit volumeUnit)
    {
        DefaultMassUnit = massUnit;
        DefaultVolumeUnit = volumeUnit;
        MarkUpdated();
    }

    void ITenantScoped.SetTenantId(Guid tenantId) => TenantId = tenantId;
}

/// <summary>
/// Per-user override of the display units. When absent, the tenant default applies.
/// </summary>
public sealed class UserUnitPreference : BaseEntity, ITenantScoped
{
    public Guid TenantId { get; private set; }

    public Guid UserId { get; private set; }

    public MassUnit? MassUnit { get; private set; }

    public VolumeUnit? VolumeUnit { get; private set; }

    private UserUnitPreference()
    {
    }

    public static UserUnitPreference Create(Guid tenantId, Guid userId, MassUnit? massUnit, VolumeUnit? volumeUnit)
        => new()
        {
            TenantId = tenantId,
            UserId = userId,
            MassUnit = massUnit,
            VolumeUnit = volumeUnit,
        };

    public void Set(MassUnit? massUnit, VolumeUnit? volumeUnit)
    {
        MassUnit = massUnit;
        VolumeUnit = volumeUnit;
        MarkUpdated();
    }

    void ITenantScoped.SetTenantId(Guid tenantId) => TenantId = tenantId;
}