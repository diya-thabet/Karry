using FluentAssertions;
using Karry.Application.Auth.Queries;
using Karry.Application.Common;
using Karry.Domain.Identity;
using Karry.Tests.Support;
using Xunit;

namespace Karry.Tests.Auth;

public sealed class GetCurrentSessionQueryTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly InMemoryRepository<User> _users;

    public GetCurrentSessionQueryTests()
    {
        _users = new InMemoryRepository<User>();
    }

    private User SeedOperator()
    {
        var role = Role.Create(_tenantId, SystemRoles.Operator, "Operator", null, [], _userId);
        var user = User.Create(_tenantId, EmailAddress.Create("op@kar.app"), "Olaf",
            new FakePasswordHasher().Hash("Karry#Op123"), false, role.Id, "dev-1", _userId);
        _users.Add(user);
        return user;
    }

    [Fact]
    public async Task ReturnsSessionContextFromUserAndClaims()
    {
        var user = SeedOperator();
        var session = FakeSession.Operator(_tenantId, user.Id, new HashSet<string> { "units:read", "units:write" });

        var handler = new GetCurrentSessionQueryHandler(_users, session);
        var result = await handler.Handle(new GetCurrentSessionQuery(), default);

        result.UserId.Should().Be(user.Id);
        result.Email.Should().Be("op@kar.app");
        result.Name.Should().Be("Olaf");
        result.TenantId.Should().Be(_tenantId);
        result.RoleCode.Should().Be(SystemRoles.Operator);
        result.IsPlatformAdmin.Should().BeFalse();
        result.TwoFactorEnabled.Should().BeFalse();
        result.Permissions.Should().BeEquivalentTo(new[] { "units:read", "units:write" });
    }

    [Fact]
    public async Task PlatformAdmin_HasNoTenantAndIsPlatformAdmin()
    {
        var admin = User.Create(null, EmailAddress.Create("root@kar.app"), "Root",
            new FakePasswordHasher().Hash("Karry#RootAdmin1"), true, null, "", Guid.NewGuid());
        _users.Add(admin);

        var session = new FakeSession { UserId = admin.Id, TenantId = null, RoleCode = null };

        var handler = new GetCurrentSessionQueryHandler(_users, session);
        var result = await handler.Handle(new GetCurrentSessionQuery(), default);

        result.IsPlatformAdmin.Should().BeTrue();
        result.TenantId.Should().BeNull();
        result.RoleCode.Should().BeNull();
    }

    [Fact]
    public async Task MissingSessionUser_Throws()
    {
        var handler = new GetCurrentSessionQueryHandler(_users, new FakeSession { UserId = Guid.NewGuid() });
        var act = async () => await handler.Handle(new GetCurrentSessionQuery(), default);

        await act.Should().ThrowAsync<AuthenticationException>();
    }
}
