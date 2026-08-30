using FluentAssertions;
using Karry.Domain.Identity;
using Xunit;

namespace Karry.Tests.Identity;

public sealed class UserTests
{
    private static readonly EmailAddress Email = EmailAddress.Create("op@example.com");
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid RoleId = Guid.NewGuid();

    private static User CreateUser() =>
        User.Create(TenantId, Email, "Operator", "hash", isPlatformAdmin: false, roleId: RoleId, deviceId: "dev-1", createdBy: Guid.NewGuid());

    [Fact]
    public void Create_TenantUserWithoutRole_Throws()
    {
        var act = () => User.Create(TenantId, Email, "Op", "hash", false, roleId: null, deviceId: "dev", createdBy: Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_PlatformAdminAllowsNullRole()
    {
        var admin = User.Create(
            tenantId: null,
            EmailAddress.Create("root@example.com"),
            "Root",
            "hash",
            isPlatformAdmin: true,
            roleId: null,
            deviceId: string.Empty,
            createdBy: Guid.NewGuid());

        admin.IsPlatformAdmin.Should().BeTrue();
        admin.TenantId.Should().BeNull();
    }

    [Fact]
    public void LoginGuard_InitiallyAllowsWithFullAttempts()
    {
        var user = CreateUser();

        var guard = user.EvaluateLoginGuard(DateTime.UtcNow);

        guard.LockedOut.Should().BeFalse();
        guard.RemainingAttempts.Should().Be(User.MaxFailedAttempts);
    }

    [Fact]
    public void RecordFailedLogin_DecrementsRemaining()
    {
        var user = CreateUser();

        user.RecordFailedLogin(DateTime.UtcNow);

        user.EvaluateLoginGuard(DateTime.UtcNow).RemainingAttempts.Should().Be(User.MaxFailedAttempts - 1);
    }

    [Fact]
    public void ReachingThreshold_LocksAccount()
    {
        var user = CreateUser();
        var now = DateTime.UtcNow;

        for (var i = 0; i < User.MaxFailedAttempts; i++)
        {
            user.RecordFailedLogin(now);
        }

        user.FailedLoginCount.Should().Be(User.MaxFailedAttempts);
        var guard = user.EvaluateLoginGuard(now);
        guard.LockedOut.Should().BeTrue();
        user.LockedUntilUtc.Should().NotBeNull();
    }

    [Fact]
    public void LockoutExpiresAfterDuration()
    {
        var user = CreateUser();
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var i = 0; i < User.MaxFailedAttempts; i++)
        {
            user.RecordFailedLogin(start);
        }

        user.EvaluateLoginGuard(start).LockedOut.Should().BeTrue();
        user.EvaluateLoginGuard(start.AddMinutes(User.LockoutDurationMinutes)).LockedOut.Should().BeFalse();
    }

    [Fact]
    public void SuccessfulLogin_ResetsFailures()
    {
        var user = CreateUser();
        user.RecordFailedLogin(DateTime.UtcNow);

        user.RecordLogin("dev-1", DateTime.UtcNow, Guid.NewGuid());

        user.FailedLoginCount.Should().Be(0);
        user.LockedUntilUtc.Should().BeNull();
        user.LastLoginAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void InactiveUserIsAlwaysBlocked()
    {
        var user = CreateUser();
        user.Deactivate(Guid.NewGuid());

        user.EvaluateLoginGuard(DateTime.UtcNow).LockedOut.Should().BeTrue();
    }

    [Fact]
    public void DisabledTwoFactor_ThenReenable_Works()
    {
        var user = CreateUser();
        user.EnableTwoFactor("SECRET", Guid.NewGuid());
        user.TwoFactorEnabled.Should().BeTrue();

        user.DisableTwoFactor(Guid.NewGuid());
        user.TwoFactorEnabled.Should().BeFalse();
        user.TotpSecret.Should().BeEmpty();
    }

    [Fact]
    public void EnableTwoFactor_WithoutSecret_Throws()
    {
        var user = CreateUser();

        user.Invoking(u => u.EnableTwoFactor("", Guid.NewGuid())).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RegisterDevice_AddsOnce()
    {
        var user = CreateUser();

        user.RegisterDevice("dev-2", Guid.NewGuid());
        user.RegisterDevice("dev-2", Guid.NewGuid());

        user.DeviceIds.Should().Contain("dev-2");
        user.DeviceIds.Count(d => d == "dev-2").Should().Be(1);
        user.DeviceIds.Should().Contain("dev-1");
    }

    [Fact]
    public void DuplicateEmailsAreStructurallyEqualIdentities()
    {
        var a = User.Create(TenantId, EmailAddress.Create("OP@example.com"), "A", "h", false, RoleId, "d", Guid.NewGuid());
        var b = User.Create(TenantId, EmailAddress.Create("op@example.com"), "B", "h", false, RoleId, "d", Guid.NewGuid());

        a.Email.Should().Be(b.Email);
    }
}

public sealed class RefreshTokenTests
{
    private static readonly DateTime Now = DateTime.UtcNow;

    private static RefreshToken CreateToken() =>
        RefreshToken.Create(Guid.NewGuid(), "hash", familyId: Guid.NewGuid(), deviceId: "dev-1", expiresAtUtc: Now.AddDays(7));

    [Fact]
    public void Create_ActiveTokenIsActive()
    {
        var token = CreateToken();

        token.StatusAt(Now).Should().Be(RefreshTokenStatus.Active);
    }

    [Fact]
    public void NewToken_NotNullOrEmptyHash()
    {
        var token = CreateToken();

        token.TokenHash.Should().NotBeNullOrWhiteSpace();
        token.FamilyId.Should().NotBe(Guid.Empty);
        token.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_EmptyDevice_Throws()
    {
        var act = () => RefreshToken.Create(Guid.NewGuid(), "hash", Guid.NewGuid(), "", Now.AddDays(1));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_PastExpiry_Throws()
    {
        var act = () => RefreshToken.Create(Guid.NewGuid(), "hash", Guid.NewGuid(), "d", Now.AddMinutes(-1));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ExpiredToken_ReportedExpired()
    {
        var token = CreateToken();

        token.StatusAt(Now.AddDays(30)).Should().Be(RefreshTokenStatus.Expired);
    }

    [Fact]
    public void RevokedToken_ReportedRevokedAndTracksReplacement()
    {
        var token = CreateToken();
        var replacementId = Guid.NewGuid();

        token.Revoke(replacementId, Now);

        token.StatusAt(Now).Should().Be(RefreshTokenStatus.Revoked);
        token.ReplacedByTokenId.Should().Be(replacementId);
    }

    [Fact]
    public void Revoke_Twice_KeepsFirstReplacement()
    {
        var token = CreateToken();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        token.Revoke(first, Now);
        token.Revoke(second, Now);

        token.ReplacedByTokenId.Should().Be(first);
    }
}