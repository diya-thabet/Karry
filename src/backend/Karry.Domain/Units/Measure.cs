using Karry.Domain.Common;

namespace Karry.Domain.Units;

/// <summary>
/// Value object encapsulating a quantity expressed in a physical unit. Supports the
/// dynamic <c>m³ ↔ Tonnes</c> toggle required by the field dispatch workflow.
/// </summary>
public sealed class Measure : ValueObject
{
    /// <summary>Mass of 1 short ton expressed in metric tons (1 st = 0.90718474 t).</summary>
    private const decimal MetricTonPerShortTon = 0.90718474m;

    public decimal Value { get; }

    public MeasureType Type { get; }

    public VolumeUnit VolumeUnit { get; }

    public MassUnit MassUnit { get; }

    private Measure(decimal value, MeasureType type, VolumeUnit volumeUnit, MassUnit massUnit)
    {
        Value = value;
        Type = type;
        VolumeUnit = volumeUnit;
        MassUnit = massUnit;
    }

    public static Measure CubicMeters(decimal value) => new(value, MeasureType.Volume, VolumeUnit.CubicMeter, MassUnit.MetricTon);

    public static Measure MetricTons(decimal value) => new(value, MeasureType.Mass, VolumeUnit.CubicMeter, MassUnit.MetricTon);

    public static Measure ShortTons(decimal value) => new(value, MeasureType.Mass, VolumeUnit.CubicMeter, MassUnit.ShortTon);

    /// <summary>
    /// Converts volumetric quantity to gravimetric mass using moisture-adjusted density.
    /// M = V × ρ × κ_moisture
    /// </summary>
    public Measure ToMass(decimal rhoDryTonPerM3, decimal kappaMoisture)
    {
        if (Type == MeasureType.Mass)
        {
            return this;
        }

        if (rhoDryTonPerM3 <= 0 || kappaMoisture < 1.0m)
        {
            throw new ArgumentOutOfRangeException(nameof(rhoDryTonPerM3), "Density must be > 0 and moisture factor must be >= 1.0.");
        }

        var metricTons = Value * rhoDryTonPerM3 * kappaMoisture;
        return MassUnit == MassUnit.MetricTon
            ? MetricTons(metricTons)
            : ShortTons(metricTons / MetricTonPerShortTon);
    }

    /// <summary>
    /// Converts gravimetric mass back to volume using the same moisture-adjusted density.
    /// V = M / (ρ × κ_moisture)
    /// </summary>
    public Measure ToVolume(decimal rhoDryTonPerM3, decimal kappaMoisture)
    {
        if (Type == MeasureType.Volume)
        {
            return this;
        }

        if (rhoDryTonPerM3 <= 0 || kappaMoisture < 1.0m)
        {
            throw new ArgumentOutOfRangeException(nameof(rhoDryTonPerM3), "Density must be > 0 and moisture factor must be >= 1.0.");
        }

        var metricTons = MassUnit == MassUnit.MetricTon ? Value : Value * MetricTonPerShortTon;
        return CubicMeters(metricTons / (rhoDryTonPerM3 * kappaMoisture));
    }

    public decimal ToMetricTons() => Type == MeasureType.Mass
        ? MassUnit == MassUnit.MetricTon ? Value : Value * MetricTonPerShortTon
        : throw new InvalidOperationException("Quantity is volumetric; provide density to convert.");

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
        yield return Type;
        yield return VolumeUnit;
        yield return MassUnit;
    }
}