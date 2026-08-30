using FluentValidation;

namespace Karry.Application.Units.Commands;

public sealed class ConvertMeasureRequestValidator : AbstractValidator<ConvertMeasureRequest>
{
    public ConvertMeasureRequestValidator()
    {
        RuleFor(x => x.Value).GreaterThan(0m);
        RuleFor(x => x.RhoDryTonPerM3).GreaterThan(0m);
        RuleFor(x => x.KappaMoisture).GreaterThanOrEqualTo(1.0m);
        RuleFor(x => x.FromUnit).NotEmpty().Must(BeSupportedUnit)
            .WithMessage("FromUnit must be one of: m3, t, st.");
    }

    private static bool BeSupportedUnit(string unit) =>
        unit is ConvertMeasureRequest.CubicMeter
            or ConvertMeasureRequest.MetricTon
            or ConvertMeasureRequest.ShortTon;
}