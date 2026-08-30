using FluentAssertions;
using Karry.Application.Common;
using Karry.Application.Security;
using Karry.Application.Tenants.Commands;
using Karry.Domain.Audit;
using Karry.Domain.Identity;
using Karry.Domain.Tenants;
using Karry.Domain.Units;
using Karry.Tests.Support;
using Xunit;

namespace Karry.Tests.Tenants;

public sealed class CreateTenantCommandTests
{
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task CreateTenant_ProvisionsRolesAndUnitPreferences()
    {
        Build(out var tenants, out var roles, out var users, out var tenantPrefs, out var audit);
        var handler = new CreateTenantCommandHandler(
            tenants, roles, users, tenantPrefs, audit, tenants, new FakePasswordHasher(),
            FakeSession.Admin(_tenantId, _actorId), new FakeClock());

        var response = await handler.Handle(new CreateTenantCommand(new("Alkaline Quarry", "KE", "USD")), default);

        response.Name.Should().Be("Alkaline Quarry");
        roles.Items.Select(r => r.Code).Should().BeEquivalentTo(SystemRoles.All);
        tenantPrefs.Items.Should().ContainSingle();
        audit.Items.Should().Contain(e => e.Action == "tenant.created");
    }

    [Fact]
    public async Task CreateTenant_WithAdminEmail_ProvisionsAdminUser()
    {
        Build(out var tenants, out var roles, out var users, out var tenantPrefs, out var audit);
        var handler = new CreateTenantCommandHandler(
            tenants, roles, users, tenantPrefs, audit, tenants, new FakePasswordHasher(),
            FakeSession.Admin(_tenantId, _actorId), new FakeClock());

        await handler.Handle(new CreateTenantCommand(new("Mine One", "ZM", "ZMW", "UTC", "en",
            AdminEmail: "admin@mine.one", AdminPassword: "Karry#Admin1", AdminName: "Chief")), default);

        users.Items.Should().ContainSingle(u => u.Email.Value == "admin@mine.one");
        var adminRole = roles.Items.Single(r => r.Code == SystemRoles.Admin);
        users.Items.Single().RoleId.Should().Be(adminRole.Id);
    }

    [Fact]
    public async Task CreateTenant_WithWeakAdminPassword_Rejected()
    {
        Build(out var tenants, out var roles, out var users, out var tenantPrefs, out var audit);
        var handler = new CreateTenantCommandHandler(
            tenants, roles, users, tenantPrefs, audit, tenants, new FakePasswordHasher(),
            FakeSession.Admin(_tenantId, _actorId), new FakeClock());

        var act = async () => await handler.Handle(new CreateTenantCommand(new("Mine One", "ZM", "ZMW", "UTC", "en",
            AdminEmail: "admin@mine.one", AdminPassword: "weak", AdminName: "Chief")), default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    private static void Build(
        out InMemoryRepository<Tenant> tenants,
        out InMemoryRepository<Role> roles,
        out InMemoryRepository<User> users,
        out InMemoryRepository<TenantUnitPreference> tenantPrefs,
        out InMemoryRepository<AuditLogEntry> audit)
    {
        tenants = new InMemoryRepository<Tenant>();
        roles = new InMemoryRepository<Role>();
        users = new InMemoryRepository<User>();
        tenantPrefs = new InMemoryRepository<TenantUnitPreference>();
        audit = new InMemoryRepository<AuditLogEntry>();
    }
}