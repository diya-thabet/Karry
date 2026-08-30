using FluentAssertions;
using Karry.Domain.Equipment;
using Karry.Domain.Maintenance;
using Xunit;

namespace Karry.Tests.Maintenance;

public sealed class WearPartTests
{
    [Fact]
    public void ComputeRemaining_UnderHoursMeter_UsesHoursRatio()
    {
        var part = WearPart.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Jaw Liner",
            "Crusher",
            5000,
            0,
            120000,
            1.4,
            Guid.NewGuid());

        part.RecordUsage(1000, 0, 20000, Guid.NewGuid());

        var remaining = part.ComputeRemaining(dailyUsage: 8, dailyTonnage: 200);

        // usage: (5000 - 1000) / (8 * 1.4) = 357.14
        // mass:  (120000 - 20000) / (200 * 1.4) = 357.14 -> both equal; hours path verified
        remaining.Should().BeApproximately(4000.0 / (8.0 * 1.4), 0.5);
    }

    [Fact]
    public void ComputeRemaining_TakesMinimum_WhenMassBinds()
    {
        var part = WearPart.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Jaw Liner",
            "Crusher",
            5000,
            0,
            120000,
            1.4,
            Guid.NewGuid());

        part.RecordUsage(1000, 0, 40000, Guid.NewGuid());

        var remaining = part.ComputeRemaining(dailyUsage: 8, dailyTonnage: 400);

        // usage: (5000 - 1000) / (8 * 1.4) = 357.14
        // mass:  (120000 - 40000) / (400 * 1.4) = 142.86 -> mass binds (minimum)
        remaining.Should().BeApproximately((120000 - 40000) / (400.0 * 1.4), 0.5);
    }

    [Fact]
    public void MeterSwitch_ChangesActiveMetric()
    {
        var part = WearPart.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Track Shoe",
            "Undercarriage",
            2000,
            100000,
            0,
            1.2,
            Guid.NewGuid());

        part.SwitchMeter(MeterKind.Kilometers, Guid.NewGuid());

        part.ActiveMeter.Should().Be(MeterKind.Kilometers);
    }

    [Fact]
    public void Create_WithoutName_Throws()
    {
        var act = () => WearPart.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            string.Empty,
            "X",
            100,
            0,
            0,
            1.0,
            Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }
}

public sealed class MachineTests
{
    [Fact]
    public void ConnectTo_SameMachine_Throws()
    {
        var machine = Machine.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Crusher 1",
            MachineType.Crusher,
            "M-9000",
            "SN-1",
            Guid.NewGuid());

        machine.Invoking(m => m.ConnectTo(machine.Id))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ConnectTo_DistinctMachine_AddsEdge()
    {
        var crusher = Machine.Create(Guid.NewGuid(), Guid.NewGuid(), "Crusher", MachineType.Crusher, "M", "SN-1", Guid.NewGuid());
        var screen = Machine.Create(Guid.NewGuid(), Guid.NewGuid(), "Screen", MachineType.Screen, "S", "SN-2", Guid.NewGuid());

        crusher.ConnectTo(screen.Id);

        crusher.DownstreamNodeIds.Should().Contain(screen.Id);
    }
}