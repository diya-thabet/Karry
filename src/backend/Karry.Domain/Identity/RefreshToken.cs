namespace Karry.Domain.Identity;

/// <summary>
/// Auth token lifecycle marker.
/// </summary>
public enum RefreshTokenStatus
{
    Active = 0,
    Revoked = 1,
    Expired = 2,
}

/// <summary>
/// Refresh token with rotation lineage. The raw token is stored hashed (SHA-256); only the
/// hash ever persists. Tokens form a family: each rotation revokes the parent and issues a
/// child. Reuse of a revoked token (replay) is detectable and revokes the entire family.
/// </summary>
public sealed class RefreshToken : Common.BaseEntity
{
    public const int TokenByteLength = 32;

    public Guid UserId { get; private init; }

    /// <summary>SHA-256 hash of the raw token value.</summary>
    public string TokenHash { get; private init; } = default!;

    /// <summary>Family identifier; rotated tokens inherit the family of their parent.</summary>
    public Guid FamilyId { get; private init; }

    public string DeviceId { get; private init; } = default!;

    public DateTime ExpiresAtUtc { get; private init; }

    public DateTime? RevokedAtUtc { get; private set; }

    /// <summary>Replaces-token lineage: set on the parent when a child is issued.</summary>
    public Guid? ReplacedByTokenId { get; private set; }

    public string? IpAddress { get; private init; }

    public string? UserAgent { get; private init; }

    private RefreshToken()
    {
    }

    public static RefreshToken Create(
        Guid userId,
        string tokenHash,
        Guid familyId,
        string deviceId,
        DateTime expiresAtUtc,
        string? ipAddress = null,
        string? userAgent = null)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("Token hash is required.", nameof(tokenHash));
        }

        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("Device is required.", nameof(deviceId));
        }

        if (expiresAtUtc <= DateTime.UtcNow)
        {
            throw new ArgumentException("Expiry must be in the future.", nameof(expiresAtUtc));
        }

        return new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            FamilyId = familyId == Guid.Empty ? Guid.NewGuid() : familyId,
            DeviceId = deviceId,
            ExpiresAtUtc = expiresAtUtc,
            IpAddress = ipAddress,
            UserAgent = userAgent,
        };
    }

    public RefreshTokenStatus StatusAt(DateTime utcNow)
    {
        if (RevokedAtUtc is not null)
        {
            return RefreshTokenStatus.Revoked;
        }

        if (ExpiresAtUtc <= utcNow)
        {
            return RefreshTokenStatus.Expired;
        }

        return RefreshTokenStatus.Active;
    }

    /// <summary>Marks this token revoked when a child in the same family supersedes it.</summary>
    public void Revoke(Guid replacedByTokenId, DateTime utcNow) => RevokeCore(replacedByTokenId, utcNow);

    /// <summary>Marks this token revoked as part of family-wide revocation (reuse detection).</summary>
    public void RevokeFamilyEntry(DateTime utcNow) => RevokeCore(null, utcNow);

    private void RevokeCore(Guid? replacedByTokenId, DateTime utcNow)
    {
        if (RevokedAtUtc is not null)
        {
            return;
        }

        ReplacedByTokenId = replacedByTokenId;
        RevokedAtUtc = utcNow;
        MarkUpdated();
    }
}