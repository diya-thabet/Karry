using Karry.Domain.Common;

namespace Karry.Domain.Maintenance;

public enum MeterKind
{
    Hours = 0,
    Kilometers = 1,
    CalendarDays = 2,
}

/// <summary>
/// Represents a tracked wear component (jaw liner, mantle pair, screen mesh, track shoe)
/// subject to hybrid predictive maintenance under an active <see cref="MeterKind"/>.
/// </summary>
public sealed class WearPart : BaseEntity, IAuditableEntity, ITenantScoped
{
    public Guid TenantId { get; private set; }

    public Guid MachineId { get; private set; }

    public string Name { get; private set; } = default!;

    public string Category { get; private set; } = default!;

    /// <summary>Active evaluation metric; switchable by managers on meter breakdown.</summary>
    public MeterKind ActiveMeter { get; private set; }

    public double RatingHours { get; private set; }

    public double RatingKilometers { get; private set; }

    public double RatingMetricTons { get; private set; }

    public double BondAbrasionIndex { get; private set; } = 1.0;

    public double AccumulatedHours { get; private set; }

    public double AccumulatedKilometers { get; private set; }

    public double ProcessedMetricTons { get; private set; }

    public Guid CreatedBy { get; private set; }

    public Guid? ModifiedBy { get; private set; }

    private WearPart()
    {
    }

    public static WearPart Create(
        Guid tenantId,
        Guid machineId,
        string name,
        string category,
        double ratingHours,
        double ratingKilometers,
        double ratingMetricTons,
        double bondAbrasionIndex,
        Guid createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Wear part name is required.", nameof(name));
        }

        return new WearPart
        {
            TenantId = tenantId,
            MachineId = machineId,
            Name = name.Trim(),
            Category = category.Trim(),
            ActiveMeter = MeterKind.Hours,
            RatingHours = ratingHours,
            RatingKilometers = ratingKilometers,
            RatingMetricTons = ratingMetricTons,
            BondAbrasionIndex = bondAbrasionIndex,
            CreatedBy = createdBy,
        };
    }

    public void SwitchMeter(MeterKind meter, Guid modifiedBy)
    {
        if (meter == ActiveMeter)
        {
            return;
        }

        ActiveMeter = meter;
        ModifiedBy = modifiedBy;
        MarkUpdated();
    }

    public void RecordUsage(double deltaHours, double deltaKilometers, double tonnage, Guid modifiedBy)
    {
        AccumulatedHours += deltaHours;
        AccumulatedKilometers += deltaKilometers;
        ProcessedMetricTons += tonnage;
        ModifiedBy = modifiedBy;
        MarkUpdated();
    }

    /// <summary>
    /// Remaining Useful Life (RUL) under the current meter, mirroring the codex formula:
    /// RUL_m(p) = min( (U_rating − U_accum)/ū_daily, (M_rating − M_proc)/m̄_daily ) / δ_abrasion
    /// </summary>
    public double ComputeRemaining(double dailyUsage, double dailyTonnage)
    {
        var usageRemaining = ActiveMeter switch
        {
            MeterKind.Hours => RatingHours - AccumulatedHours,
            MeterKind.Kilometers => RatingKilometers - AccumulatedKilometers,
            _ => double.MaxValue,
        };

        var massRemaining = RatingMetricTons - ProcessedMetricTons;

        var dailyRate = ActiveMeter == MeterKind.CalendarDays ? dailyUsage : dailyUsage;
        var usageDaysLeft = usageRemaining / (dailyRate * BondAbrasionIndex);
        var massDaysLeft = massRemaining / (dailyTonnage * BondAbrasionIndex);

        return Math.Max(0, Math.Min(usageDaysLeft, massDaysLeft));
    }

    void ITenantScoped.SetTenantId(Guid tenantId)
    {
        TenantId = tenantId;
    }
}