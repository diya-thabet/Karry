using FluentAssertions;
using Karry.Application.Auth.Commands;
using Karry.Application.Common;
using Karry.Application.Security;
using Karry.Domain.Audit;
using Karry.Domain.Identity;
using Karry.Domain.Tenants;
using Karry.Tests.Support;
using Xunit;

namespace Karry.Tests.Auth;

public sealed class LoginCommandTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FakeClock _clock;
    private readonly InMemoryRepository<User> _users;
    private readonly InMemoryRepository<Role> _roles;
    private readonly InMemoryRepository<Tenant> _tenants;
    private readonly InMemoryRepository<AuditLogEntry> _audit;
    private readonly FakeTokenIssuer _tokenIssuer;

    public LoginCommandTests()
    {
        _clock = new FakeClock();
        _users = new InMemoryRepository<User>();
        _roles = new InMemoryRepository<Role>();
        _tenants = new InMemoryRepository<Tenant>();
        _audit = new InMemoryRepository<AuditLogEntry>();
        _tokenIssuer = new FakeTokenIssuer();
    }

    private LoginCommandHandler CreateHandler() => new(
        _users, _roles, _tenants, _audit,
        _users, new FakePasswordHasher(), _tokenIssuer, _clock);

    private User SeedActiveOperator()
    {
        var role = Role.Create(_tenantId, SystemRoles.Operator, "Operator", null, PermissionCatalog.ForRole(SystemRoles.Operator)
            .SelectMany(kv => kv.Value.Select(a => Permission.Create(kv.Key, a))).ToList(), _userId);
        var olaf = User.Create(_tenantId, EmailAddress.Create("op@kar.app"), "Olaf",
            new FakePasswordHasher().Hash("Karry#Op123"), false, role.Id, "dev-1", _userId);
        _roles.Add(role);
        _users.Add(olaf);
        return olaf;
    }

    [Fact]
    public async Task ValidCredentials_ReturnAccessAndRefreshTokens()
    {
        var olaf = SeedActiveOperator();

        var response = await CreateHandler().Handle(new LoginCommand(new("op@kar.app", "Karry#Op123", "dev-1")), default);

        response.RequiresTwoFactor.Should().BeFalse();
        response.Tokens.Should().NotBeNull();
        response.Tokens!.AccessToken.Should().StartWith("access.");
        response.Tokens.RefreshToken.Should().StartWith("refresh.");
        response.RoleCode.Should().Be(SystemRoles.Operator);

        var persisted = await _users.GetByIdAsync(olaf.Id);
        persisted!.FailedLoginCount.Should().Be(0);
        _audit.Items.Should().Contain(e => e.Action == "login.succeeded");
    }

    [Fact]
    public async Task WrongPassword_IncrementsFailureCount()
    {
        var olaf = SeedActiveOperator();

        var act = async () => await CreateHandler().Handle(new LoginCommand(new("op@kar.app", "WrongPass!1", "dev-1")), default);

        await act.Should().ThrowAsync<AuthenticationException>();
        (await _users.GetByIdAsync(olaf.Id))!.FailedLoginCount.Should().Be(1);
        _audit.Items.Should().Contain(e => e.Action == "login.failed.password");
    }

    [Fact]
    public async Task FiveFailures_LockAccount()
    {
        var olaf = SeedActiveOperator();
        var handler = CreateHandler();

        for (var i = 0; i < User.MaxFailedAttempts; i++)
        {
            try
            {
                await handler.Handle(new LoginCommand(new("op@kar.app", "WrongPass!1", "dev-1")), default);
            }
            catch (AuthenticationException)
            {
            }
            catch (AccountLockedException)
            {
            }
        }

        (await _users.GetByIdAsync(olaf.Id))!.LockedUntilUtc.Should().NotBeNull();
        var act = async () => await handler.Handle(new LoginCommand(new("op@kar.app", "Karry#Op123", "dev-1")), default);
        await act.Should().ThrowAsync<AccountLockedException>();
    }

    [Fact]
    public async Task InactiveAccount_Rejected()
    {
        var olaf = SeedActiveOperator();
        olaf.Deactivate(_userId);

        var act = async () => await CreateHandler().Handle(new LoginCommand(new("op@kar.app", "Karry#Op123", "dev-1")), default);

        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*not active*");
    }

    [Fact]
    public async Task TwoFactorEnabled_RequiresChallenge()
    {
        var olaf = SeedActiveOperator();
        olaf.EnableTwoFactor("SECRET", _userId);

        var response = await CreateHandler().Handle(new LoginCommand(new("op@kar.app", "Karry#Op123", "dev-1")), default);

        response.RequiresTwoFactor.Should().BeTrue();
        response.Tokens.Should().BeNull();
        response.ChallengeToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UnknownEmail_Throws()
    {
        var act = async () => await CreateHandler().Handle(new LoginCommand(new("nobody@kar.app", "Karry#Op123", "dev-1")), default);
        await act.Should().ThrowAsync<AuthenticationException>();
    }
}