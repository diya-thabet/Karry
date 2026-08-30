using FluentAssertions;
using Karry.Application.Common;
using Karry.Application.Users.Commands;
using Karry.Domain.Identity;
using Karry.Tests.Support;
using Xunit;

namespace Karry.Tests.Users;

public sealed class CreateUserCommandTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();

    [Fact]
    public async Task CreateUser_WithValidRoleAndPassword_Succeeds()
    {
        var role = Role.Create(_tenantId, SystemRoles.Operator, "Operator", null, [], _actorId);
        var roles = new InMemoryRepository<Role>([role]);
        var users = new InMemoryRepository<User>();

        var handler = new CreateUserCommandHandler(
            users, roles, users, new FakePasswordHasher(), FakeSession.Admin(_tenantId, _actorId));

        var response = await handler.Handle(new CreateUserCommand(
            new("op@kar.app", "Olaf", "Karry#Op123", role.Id)), default);

        response.UserId.Should().NotBeEmpty();
        var user = users.Items.Single();
        user.TenantId.Should().Be(_tenantId);
        user.RoleId.Should().Be(role.Id);
    }

    [Fact]
    public async Task CreateUser_WithRoleFromAnotherTenant_Forbidden()
    {
        var role = Role.Create(Guid.NewGuid(), SystemRoles.Operator, "Operator", null, [], _actorId);
        var roles = new InMemoryRepository<Role>([role]);
        var users = new InMemoryRepository<User>();

        var handler = new CreateUserCommandHandler(
            users, roles, users, new FakePasswordHasher(), FakeSession.Admin(_tenantId, _actorId));

        var act = async () => await handler.Handle(new CreateUserCommand(
            new("op@kar.app", "Olaf", "Karry#Op123", role.Id)), default);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_Conflict()
    {
        var role = Role.Create(_tenantId, SystemRoles.Operator, "Operator", null, [], _actorId);
        var roles = new InMemoryRepository<Role>([role]);
        var existing = User.Create(_tenantId, EmailAddress.Create("op@kar.app"), "Existing",
            "hash", false, role.Id, string.Empty, _actorId);
        var users = new InMemoryRepository<User>([existing]);

        var handler = new CreateUserCommandHandler(
            users, roles, users, new FakePasswordHasher(), FakeSession.Admin(_tenantId, _actorId));

        var act = async () => await handler.Handle(new CreateUserCommand(
            new("OP@kar.app", "New", "Karry#Op123", role.Id)), default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CreateUser_WeakPassword_Conflict()
    {
        var role = Role.Create(_tenantId, SystemRoles.Operator, "Operator", null, [], _actorId);
        var roles = new InMemoryRepository<Role>([role]);
        var users = new InMemoryRepository<User>();

        var handler = new CreateUserCommandHandler(
            users, roles, users, new FakePasswordHasher(), FakeSession.Admin(_tenantId, _actorId));

        var act = async () => await handler.Handle(new CreateUserCommand(
            new("op@kar.app", "Olaf", "short", role.Id)), default);

        await act.Should().ThrowAsync<ConflictException>();
    }
}