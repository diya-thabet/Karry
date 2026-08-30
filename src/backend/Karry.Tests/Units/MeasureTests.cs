using FluentAssertions;
using Karry.Domain.Units;
using Xunit;

namespace Karry.Tests.Units;

public sealed class MeasureTests
{
    private const decimal GraniteDensity = 2.65m;
    private const decimal Moisture = 1.10m;

    [Fact]
    public void CubicMeters_ToMass_AppliesDensityAndMoisture()
    {
        var measure = Measure.CubicMeters(100m);

        var mass = measure.ToMass(GraniteDensity, Moisture);

        mass.Type.Should().Be(MeasureType.Mass);
        mass.MassUnit.Should().Be(MassUnit.MetricTon);
        mass.Value.Should().Be(100m * GraniteDensity * Moisture);
    }

    [Fact]
    public void ToMass_ThenToVolume_IsRoundTrip()
    {
        var original = Measure.CubicMeters(250m);

        var mass = original.ToMass(GraniteDensity, Moisture);
        var volume = mass.ToVolume(GraniteDensity, Moisture);

        volume.Value.Should().BeApproximately(original.Value, 0.001m);
    }

    [Fact]
    public void ShortTons_ToMetricTons_ConvertsCorrectly()
    {
        var shortTons = Measure.ShortTons(1.102311310924388m);

        shortTons.ToMetricTons().Should().BeApproximately(1.0m, 0.0000001m);
    }

    [Fact]
    public void ToMass_WithInvalidDensity_Throws()
    {
        var measure = Measure.CubicMeters(10m);

        measure.Invoking(m => m.ToMass(0m, Moisture))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(1.25)]
    public void ToMass_RejectsMoistureBelowOne(double kappa)
    {
        var measure = Measure.CubicMeters(10m);

        if (kappa < 1.0)
        {
            measure.Invoking(m => m.ToMass(GraniteDensity, (decimal)kappa))
                .Should().Throw<ArgumentOutOfRangeException>();
        }
        else
        {
            measure.Invoking(m => m.ToMass(GraniteDensity, (decimal)kappa))
                .Should().NotThrow();
        }
    }
}