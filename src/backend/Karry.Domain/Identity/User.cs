namespace Karry.Domain.Identity;

/// <summary>
/// Result of a failed-login attempt evaluation.
/// </summary>
public sealed record LoginGuardResult(bool LockedOut, int RemainingAttempts);

/// <summary>
/// User aggregate: credentials, security state (account lockout, optional TOTP 2FA),
/// device bindings, and audit metadata. Tenant scoped except platform super admins
/// (TenantId == null).
/// </summary>
public sealed class User : Common.BaseEntity, Common.IAuditableEntity
{
    public const int MaxFailedAttempts = 5;

    public const int LockoutDurationMinutes = 15;

    public Guid? TenantId { get; private set; }

    public EmailAddress Email { get; private set; } = default!;

    public string Name { get; private set; } = default!;

    public string PasswordHash { get; private set; } = default!;

    public bool IsActive { get; private set; } = true;

    public bool IsPlatformAdmin { get; private set; }

    public int FailedLoginCount { get; private set; }

    public DateTime? LockedUntilUtc { get; private set; }

    public bool TwoFactorEnabled { get; private set; }

    /// <summary>Base32 TOTP secret; empty when 2FA is disabled.</summary>
    public string TotpSecret { get; private set; } = "";

    /// <summary>Role reference. Null for platform super admins.</summary>
    public Guid? RoleId { get; private set; }

    private readonly List<string> _deviceIds = [];
    public IReadOnlyList<string> DeviceIds => _deviceIds.AsReadOnly();

    public Guid CreatedBy { get; private set; }

    public Guid? ModifiedBy { get; private set; }

    public DateTime? LastLoginAtUtc { get; private set; }

    private User()
    {
    }

    public static User Create(
        Guid? tenantId,
        EmailAddress email,
        string name,
        string passwordHash,
        bool isPlatformAdmin,
        Guid? roleId,
        string deviceId,
        Guid createdBy)
    {
        ArgumentNullException.ThrowIfNull(email);

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("User name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        if (!isPlatformAdmin && roleId is null)
        {
            throw new ArgumentException("A tenant user must have a role.", nameof(roleId));
        }

        var user = new User
        {
            TenantId = tenantId,
            Email = email,
            Name = name.Trim(),
            PasswordHash = passwordHash,
            IsPlatformAdmin = isPlatformAdmin,
            RoleId = roleId,
            CreatedBy = createdBy,
        };

        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            user._deviceIds.Add(deviceId.Trim());
        }

        return user;
    }

    /// <summary>
    /// Guards a login attempt for a specific device. Enforces the account-lockout policy:
    /// after <see cref="MaxFailedAttempts"/> consecutive failures the account is locked for
    /// <see cref="LockoutDurationMinutes"/> minutes. Returns whether the user may attempt login.
    /// </summary>
    public LoginGuardResult EvaluateLoginGuard(DateTime utcNow)
    {
        if (!IsActive)
        {
            return new LoginGuardResult(true, 0);
        }

        if (LockedUntilUtc is not null && LockedUntilUtc > utcNow)
        {
            return new LoginGuardResult(true, 0);
        }

        return new LoginGuardResult(false, Math.Max(0, MaxFailedAttempts - FailedLoginCount));
    }

    /// <summary>Records a failed password attempt; locks the account once the threshold is reached.</summary>
    public void RecordFailedLogin(DateTime utcNow)
    {
        FailedLoginCount += 1;

        if (FailedLoginCount >= MaxFailedAttempts)
        {
            LockedUntilUtc = utcNow.AddMinutes(LockoutDurationMinutes);
        }

        MarkUpdated();
    }

    /// <summary>Resets the failure counter and any active lockout after a successful login.</summary>
    public void ResetFailedLogins()
    {
        FailedLoginCount = 0;
        LockedUntilUtc = null;
        MarkUpdated();
    }

    public void EnableTwoFactor(string base32Secret, Guid modifiedBy)
    {
        if (string.IsNullOrWhiteSpace(base32Secret))
        {
            throw new ArgumentException("TOTP secret is required.", nameof(base32Secret));
        }

        TwoFactorEnabled = true;
        TotpSecret = base32Secret;
        ModifiedBy = modifiedBy;
        MarkUpdated();
    }

    public void DisableTwoFactor(Guid modifiedBy, string? deviceId = null)
    {
        TwoFactorEnabled = false;
        TotpSecret = string.Empty;
        ModifiedBy = modifiedBy;
        MarkUpdated();
    }

    public void RegisterDevice(string deviceId, Guid modifiedBy)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("Device is required.", nameof(deviceId));
        }

        deviceId = deviceId.Trim();

        if (!_deviceIds.Contains(deviceId))
        {
            _deviceIds.Add(deviceId);
            ModifiedBy = modifiedBy;
            MarkUpdated();
        }
    }

    public void RecordLogin(string deviceId, DateTime utcNow, Guid modifiedBy)
    {
        ResetFailedLogins();
        LastLoginAtUtc = utcNow;
        RegisterDevice(deviceId, modifiedBy);
    }

    public void UpdatePassword(string newPasswordHash, Guid modifiedBy)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(newPasswordHash));
        }

        PasswordHash = newPasswordHash;
        ModifiedBy = modifiedBy;
        MarkUpdated();
    }

    public void Deactivate(Guid modifiedBy)
    {
        IsActive = false;
        ModifiedBy = modifiedBy;
        MarkUpdated();
    }

    public void Reactivate(Guid modifiedBy)
    {
        IsActive = true;
        ResetFailedLogins();
        ModifiedBy = modifiedBy;
        MarkUpdated();
    }
}