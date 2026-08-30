using FluentAssertions;
using Karry.Application.Auth.Commands;
using Karry.Application.Common;
using Karry.Application.Security;
using Karry.Domain.Audit;
using Karry.Domain.Identity;
using Karry.Tests.Support;
using Xunit;

namespace Karry.Tests.Auth;

public sealed class RefreshCommandTests
{
    private readonly Karry.Domain.Identity.RefreshToken _parent;
    private readonly InMemoryRepository<Karry.Domain.Identity.RefreshToken> _tokens;
    private readonly InMemoryRepository<User> _users;
    private readonly InMemoryRepository<Role> _roles;
    private readonly InMemoryRepository<AuditLogEntry> _audit;
    private readonly FakeClock _clock = new();

    public RefreshCommandTests()
    {
        _tokens = new InMemoryRepository<Karry.Domain.Identity.RefreshToken>();
        _users = new InMemoryRepository<User>();
        _roles = new InMemoryRepository<Role>();

        var tenantId = Guid.NewGuid();
        var role = Role.Create(tenantId, SystemRoles.Operator, "Operator", null,
            PermissionCatalog.ForRole(SystemRoles.Operator).SelectMany(kv => kv.Value.Select(a => Permission.Create(kv.Key, a))).ToList(), Guid.NewGuid());
        var user = User.Create(tenantId, EmailAddress.Create("op@kar.app"), "Olaf",
            new FakePasswordHasher().Hash("Karry#Op123"), false, role.Id, "dev-1", Guid.NewGuid());
        _roles.Add(role);
        _users.Add(user);

        _parent = Karry.Domain.Identity.RefreshToken.Create(
            user.Id, RefreshTokenHasher.Hash("parent-raw"), familyId: Guid.NewGuid(), "dev-1", DateTime.UtcNow.AddDays(30));
        _tokens.Add(_parent);

        _audit = new InMemoryRepository<AuditLogEntry>();
    }

    private RefreshCommandHandler CreateHandler() => new(
        _tokens, _users, _roles, _audit, _tokens, new FakeTokenIssuer(_tokens), _clock);

    [Fact]
    public async Task ActiveToken_RotatesIntoSameFamily()
    {
        var response = await CreateHandler().Handle(new RefreshCommand(new("parent-raw", "dev-1")), default);

        response.RefreshToken.Should().NotBeNullOrEmpty();
        _parent.StatusAt(_clock.UtcNow).Should().Be(Karry.Domain.Identity.RefreshTokenStatus.Revoked);
        _tokens.Items.Count(t => t.FamilyId == _parent.FamilyId).Should().Be(2);
    }

    [Fact]
    public async Task RevokedToken_RevokesEntireFamily()
    {
        _parent.RevokeFamilyEntry(_clock.UtcNow);

        var act = async () => await CreateHandler().Handle(new RefreshCommand(new("parent-raw", "dev-1")), default);

        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*revoked*");
    }

    [Fact]
    public async Task ExpiredToken_Throws()
    {
        var expired = Karry.Domain.Identity.RefreshToken.Create(
            _parent.UserId, RefreshTokenHasher.Hash("expired-raw"), familyId: Guid.NewGuid(), "dev-1", DateTime.UtcNow.AddMinutes(5));
        _tokens.Add(expired);

        // Advance the clock past the token's expiry.
        _clock.UtcNow = DateTime.UtcNow.AddDays(1);

        var act = async () => await CreateHandler().Handle(new RefreshCommand(new("expired-raw", "dev-1")), default);

        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*expired*");
    }

    [Fact]
    public async Task UnknownToken_Throws()
    {
        var act = async () => await CreateHandler().Handle(new RefreshCommand(new("never-existed", "dev-1")), default);
        await act.Should().ThrowAsync<AuthenticationException>();
    }
}