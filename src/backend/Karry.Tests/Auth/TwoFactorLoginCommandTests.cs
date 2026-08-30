using FluentAssertions;
using Karry.Application.Auth.Commands;
using Karry.Application.Common;
using Karry.Application.Security;
using Karry.Domain.Audit;
using Karry.Domain.Identity;
using Karry.Tests.Support;
using Xunit;

namespace Karry.Tests.Auth;

public sealed class TwoFactorLoginCommandTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FakeClock _clock = new();
    private readonly InMemoryRepository<User> _users;
    private readonly InMemoryRepository<Role> _roles;
    private readonly InMemoryRepository<AuditLogEntry> _audit;

    public TwoFactorLoginCommandTests()
    {
        _users = new InMemoryRepository<User>();
        _roles = new InMemoryRepository<Role>();
        _audit = new InMemoryRepository<AuditLogEntry>();
    }

    private TwoFactorLoginCommandHandler CreateHandler() => new(
        _users, _roles, _audit, _users, new FakeTotp(), new FakeTokenIssuer(), _clock);

    private User SeedUserWithTwoFactor()
    {
        var tenantId = Guid.NewGuid();
        var role = Role.Create(tenantId, SystemRoles.Operator, "Operator", null, [], _userId);
        var user = User.Create(tenantId, EmailAddress.Create("op@kar.app"), "Olaf",
            new FakePasswordHasher().Hash("Karry#Op123"), false, role.Id, "dev-1", _userId);
        user.EnableTwoFactor("SECRET", _userId);
        _roles.Add(role);
        _users.Add(user);
        return user;
    }

    [Fact]
    public async Task ValidCode_ReturnsAccessAndRefreshTokens()
    {
        SeedUserWithTwoFactor();

        var response = await CreateHandler().Handle(
            new TwoFactorLoginCommand(new("op@kar.app", "123456", "dev-1")), default);

        response.RequiresTwoFactor.Should().BeFalse();
        response.Tokens.Should().NotBeNull();
        _audit.Items.Should().Contain(e => e.Action == "login.2fa.succeeded");
    }

    [Fact]
    public async Task InvalidCode_ThrowsAndCountsFailure()
    {
        var olaf = SeedUserWithTwoFactor();
        var fakeTotp = new InvalidTotp();

        var handler = new TwoFactorLoginCommandHandler(
            _users, _roles, _audit, _users, fakeTotp, new FakeTokenIssuer(), _clock);
        var act = async () => await handler.Handle(
            new TwoFactorLoginCommand(new("op@kar.app", "000000", "dev-1")), default);

        await act.Should().ThrowAsync<AuthenticationException>();
        (await _users.GetByIdAsync(olaf.Id))!.FailedLoginCount.Should().Be(1);
        _audit.Items.Should().Contain(e => e.Action == "login.2fa.failed");
    }

    [Fact]
    public async Task TwoFactorNotEnabled_Throws()
    {
        var tenantId = Guid.NewGuid();
        var role = Role.Create(tenantId, SystemRoles.Operator, "Operator", null, [], _userId);
        var user = User.Create(tenantId, EmailAddress.Create("op@kar.app"), "Olaf",
            new FakePasswordHasher().Hash("Karry#Op123"), false, role.Id, "dev-1", _userId);
        _roles.Add(role);
        _users.Add(user);

        var handler = new TwoFactorLoginCommandHandler(
            _users, _roles, _audit, _users, new FakeTotp(), new FakeTokenIssuer(), _clock);
        var act = async () => await handler.Handle(
            new TwoFactorLoginCommand(new("op@kar.app", "123456", "dev-1")), default);

        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*not enabled*");
    }

    private sealed class InvalidTotp : FakeTotp
    {
        public override bool Validate(string secret, string code, TimeSpan clockSkew) => false;
    }
}