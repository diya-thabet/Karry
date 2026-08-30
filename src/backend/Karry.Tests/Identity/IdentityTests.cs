using FluentAssertions;
using Karry.Domain.Identity;
using Xunit;

namespace Karry.Tests.Identity;

public sealed class EmailAddressTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("  spaced@example.com  ")]
    [InlineData("first.last+tag@sub.example.co")]
    public void Create_ValidEmail_TrimsAndNormalizes(string email)
    {
        var result = EmailAddress.Create(email);

        result.Value.Should().Be(email.Trim().ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_NullOrWhitespace_Throws(string? email)
    {
        var act = () => EmailAddress.Create(email!);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("a@b@c.com")]
    [InlineData("no-at-sign")]
    [InlineData("@missinglocal.com")]
    [InlineData("missingdomain@")]
    [InlineData("with space@example.com")]
    [InlineData("a@b")]
    public void Create_Malformed_Throws(string email)
    {
        var act = () => EmailAddress.Create(email);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_IsCaseInsensitiveByNormalization()
    {
        var a = EmailAddress.Create("User@Example.COM");
        var b = EmailAddress.Create("user@example.com");

        a.Should().Be(b);
    }
}

public sealed class PasswordPolicyTests
{
    [Fact]
    public void Validate_StrongPassword_Passes()
    {
        var result = PasswordPolicy.Validate("Str0ng!Pass");

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("short1!A", "at least 10")]
    [InlineData("alllowercase1!", "uppercase")]
    [InlineData("ALLUPPERCASE1!", "lowercase")]
    [InlineData("NoDigitsHere!", "digit")]
    [InlineData("NoSpecial1Char", "special")]
    [InlineData("", "required")]
    [InlineData(null, "required")]
    public void Validate_WeakPassword_ReportsReason(string? password, string expectedIssue)
    {
        var result = PasswordPolicy.Validate(password);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains(expectedIssue, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_TooLongPassword_Fails()
    {
        var longPassword = new string('A', 100) + "1!" + new string('a', 30);

        PasswordPolicy.Validate(longPassword).IsValid.Should().BeFalse();
    }
}

public sealed class RolePermissionTests
{
    private static Permission P(string resource, PermissionAction action) => Permission.Create(resource, action);

    [Fact]
    public void Grant_DuplicatePermission_IsIdempotent()
    {
        var role = Role.Create(Guid.NewGuid(), "admin", "Admin", null, [P("units", PermissionAction.Read)], Guid.NewGuid());

        role.Grant(P("units", PermissionAction.Read), Guid.NewGuid());

        role.Permissions.Should().ContainSingle();
    }

    [Fact]
    public void Revoke_RemovesPermission()
    {
        var units = P("units", PermissionAction.Read);
        var role = Role.Create(Guid.NewGuid(), "admin", "Admin", null, [units], Guid.NewGuid());

        role.Revoke(units.Id, Guid.NewGuid());

        role.Permissions.Should().BeEmpty();
    }

    [Fact]
    public void HasPermission_MatchesResourceAndAction()
    {
        var role = Role.Create(Guid.NewGuid(), "admin", "Admin", null,
            [P("ledger", PermissionAction.Mask)], Guid.NewGuid());

        role.HasPermission("ledger", PermissionAction.Mask).Should().BeTrue();
        role.HasPermission("ledger", PermissionAction.Read).Should().BeFalse();
        role.HasPermission("LEDGER", PermissionAction.Mask).Should().BeTrue();
    }

    [Fact]
    public void Create_WithoutCode_Throws()
    {
        var act = () => Role.Create(Guid.NewGuid(), "", "Admin", null, [], Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }
}

public sealed class PermissionCatalogTests
{
    [Fact]
    public void AllSystemRolesHaveRowInMatrix()
    {
        foreach (var code in SystemRoles.All)
        {
            PermissionCatalog.ForRole(code).Should().NotBeEmpty($"role {code} must be seeded");
        }
    }

    [Fact]
    public void EveryRoleGrantsOnlyKnownResources()
    {
        var known = new[]
        {
            Resources.Units, Resources.Tenants, Resources.Users, Resources.Roles,
            Resources.Machines, Resources.WearParts, Resources.Shifts, Resources.ScaleTickets,
            Resources.Warehouse, Resources.Ledger, Resources.Audit, Resources.Maintenance,
        };

        foreach (var role in PermissionCatalog.RoleCodes)
        {
            foreach (var resource in PermissionCatalog.ForRole(role).Keys)
            {
                known.Should().Contain(resource);
            }
        }
    }

    [Fact]
    public void AdminHasWriteOnCriticalResources()
    {
        PermissionCatalog.HasGrant(SystemRoles.Admin, Resources.Users, PermissionAction.Write).Should().BeTrue();
        PermissionCatalog.HasGrant(SystemRoles.Admin, Resources.Ledger, PermissionAction.Write).Should().BeTrue();
        PermissionCatalog.HasGrant(SystemRoles.Admin, Resources.Tenants, PermissionAction.Write).Should().BeTrue();
    }

    [Theory]
    [InlineData(SystemRoles.Executive, Resources.Ledger, PermissionAction.Read)]
    [InlineData(SystemRoles.Operator, Resources.Shifts, PermissionAction.Write)]
    [InlineData(SystemRoles.Weighmaster, Resources.ScaleTickets, PermissionAction.Write)]
    [InlineData(SystemRoles.Storekeeper, Resources.Warehouse, PermissionAction.Write)]
    public void CoreGrantsExist(string role, string resource, PermissionAction action)
    {
        PermissionCatalog.HasGrant(role, resource, action).Should().BeTrue();
    }

    [Fact]
    public void OperatorCannotReadLedgerUnmasked()
    {
        PermissionCatalog.HasGrant(SystemRoles.Operator, Resources.Ledger, PermissionAction.Read).Should().BeFalse();
    }

    [Fact]
    public void ExecutiveHasMaskedUserReadNotFullWriteAccess()
    {
        PermissionCatalog.ForRole(SystemRoles.Executive)[Resources.Users]
            .Should().Contain(PermissionAction.Mask)
            .And.NotContain(PermissionAction.Write);
    }
}