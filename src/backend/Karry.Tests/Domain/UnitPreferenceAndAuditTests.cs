using FluentAssertions;
using Karry.Domain.Audit;
using Karry.Domain.Units;
using Xunit;

namespace Karry.Tests.Domain;

public sealed class UnitPreferenceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void TenantPreference_DefaultsToMetric()
    {
        var pref = TenantUnitPreference.Create(TenantId, MassUnit.MetricTon, VolumeUnit.CubicMeter);

        pref.DefaultMassUnit.Should().Be(MassUnit.MetricTon);
        pref.DefaultVolumeUnit.Should().Be(VolumeUnit.CubicMeter);
    }

    [Fact]
    public void TenantPreference_CanSwitchDefaultMassUnit()
    {
        var pref = TenantUnitPreference.Create(TenantId, MassUnit.MetricTon, VolumeUnit.CubicMeter);

        pref.SetDefaults(MassUnit.ShortTon, VolumeUnit.CubicMeter);

        pref.DefaultMassUnit.Should().Be(MassUnit.ShortTon);
        pref.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void UserPreference_PartialOverrideAllowsNullComponents()
    {
        var pref = UserUnitPreference.Create(TenantId, Guid.NewGuid(), massUnit: null, volumeUnit: VolumeUnit.CubicMeter);

        pref.MassUnit.Should().BeNull();
        pref.VolumeUnit.Should().Be(VolumeUnit.CubicMeter);
    }

    [Fact]
    public void UserPreference_UpdateReplacesValues()
    {
        var pref = UserUnitPreference.Create(TenantId, Guid.NewGuid(), MassUnit.ShortTon, null);

        pref.Set(MassUnit.MetricTon, VolumeUnit.CubicMeter);

        pref.MassUnit.Should().Be(MassUnit.MetricTon);
        pref.VolumeUnit.Should().Be(VolumeUnit.CubicMeter);
    }
}

public sealed class AuditLogEntryTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Create_StampsOccurredAtAndOutcome()
    {
        var entry = AuditLogEntry.Create(TenantId, UserId, "user.login", "users", Guid.NewGuid().ToString(), "before", "after", AuditOutcome.Succeeded);

        entry.Outcome.Should().Be(AuditOutcome.Succeeded);
        entry.Action.Should().Be("user.login");
        entry.OccurredAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_NullTenant_Throws()
    {
        var act = () => AuditLogEntry.Create(null, UserId, "action", null, null, null, null, AuditOutcome.Succeeded);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithoutAction_Throws()
    {
        var act = () => AuditLogEntry.Create(TenantId, UserId, "", null, null, null, null, AuditOutcome.Succeeded);

        act.Should().Throw<ArgumentException>();
    }
}