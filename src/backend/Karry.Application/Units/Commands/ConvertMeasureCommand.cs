using Karry.Domain.Units;
using MediatR;

namespace Karry.Application.Units.Commands;

public sealed class ConvertMeasureCommand : IRequest<ConvertMeasureResponse>
{
    public required ConvertMeasureRequest Input { get; init; }
}

public sealed class ConvertMeasureCommandHandler : IRequestHandler<ConvertMeasureCommand, ConvertMeasureResponse>
{
    public Task<ConvertMeasureResponse> Handle(ConvertMeasureCommand request, CancellationToken cancellationToken)
    {
        var i = request.Input;

        var measure = i.FromUnit switch
        {
            ConvertMeasureRequest.CubicMeter => Measure.CubicMeters(i.Value),
            ConvertMeasureRequest.MetricTon => Measure.MetricTons(i.Value),
            ConvertMeasureRequest.ShortTon => Measure.ShortTons(i.Value),
            _ => throw new InvalidOperationException($"Unit '{i.FromUnit}' is not supported."),
        };

        Measure result;
        string toUnit;

        if (measure.Type == MeasureType.Volume)
        {
            result = measure.ToMass(i.RhoDryTonPerM3, i.KappaMoisture);
            toUnit = result.MassUnit == MassUnit.MetricTon ? ConvertMeasureRequest.MetricTon : ConvertMeasureRequest.ShortTon;
        }
        else
        {
            result = measure.ToVolume(i.RhoDryTonPerM3, i.KappaMoisture);
            toUnit = ConvertMeasureRequest.CubicMeter;
        }

        return Task.FromResult(new ConvertMeasureResponse(
            result.Value,
            toUnit,
            i.RhoDryTonPerM3,
            i.KappaMoisture));
    }
}