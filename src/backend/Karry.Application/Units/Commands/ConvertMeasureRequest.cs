namespace Karry.Application.Units.Commands;

public sealed record ConvertMeasureRequest(
    decimal Value,
    string FromUnit,
    decimal RhoDryTonPerM3,
    decimal KappaMoisture = 1.0m)
{
    public const string CubicMeter = "m3";

    public const string MetricTon = "t";

    public const string ShortTon = "st";
}

public sealed record ConvertMeasureResponse(
    decimal Value,
    string ToUnit,
    decimal AppliedDensity,
    decimal AppliedMoistureFactor);