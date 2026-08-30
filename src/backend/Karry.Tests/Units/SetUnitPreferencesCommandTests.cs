using FluentAssertions;
using Karry.Application.Units.Commands;
using Karry.Domain.Units;
using Karry.Tests.Support;
using Xunit;

namespace Karry.Tests.Units;

public sealed class SetUnitPreferencesCommandTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public async Task SetPreferences_CreatesRow_WhenNoneExists()
    {
        var prefs = new InMemoryRepository<UserUnitPreference>();

        var handler = new SetUnitPreferencesCommandHandler(prefs, FakeSession.Admin(_tenantId, _userId), prefs);

        await handler.Handle(new SetUnitPreferencesCommand(new("st", "m3")), default);

        var row = prefs.Items.Single();
        row.TenantId.Should().Be(_tenantId);
        row.UserId.Should().Be(_userId);
        row.MassUnit.Should().Be(MassUnit.ShortTon);
        row.VolumeUnit.Should().Be(VolumeUnit.CubicMeter);
    }

    [Fact]
    public async Task SetPreferences_UpdatesExistingRow()
    {
        var existing = UserUnitPreference.Create(_tenantId, _userId, MassUnit.MetricTon, VolumeUnit.CubicMeter);
        var prefs = new InMemoryRepository<UserUnitPreference>([existing]);

        var handler = new SetUnitPreferencesCommandHandler(prefs, FakeSession.Admin(_tenantId, _userId), prefs);

        await handler.Handle(new SetUnitPreferencesCommand(new("st", "m3")), default);

        prefs.Items.Should().ContainSingle();
        var row = prefs.Items.Single();
        row.MassUnit.Should().Be(MassUnit.ShortTon);
    }

    [Fact]
    public async Task SetPreferences_UnknownMassUnit_Throws()
    {
        var prefs = new InMemoryRepository<UserUnitPreference>();
        var handler = new SetUnitPreferencesCommandHandler(prefs, FakeSession.Admin(_tenantId, _userId), prefs);

        var act = async () => await handler.Handle(new SetUnitPreferencesCommand(new("lb", "m3")), default);

        await act.Should().ThrowAsync<Application.Common.ConflictException>();
    }
}