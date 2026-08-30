using Karry.Application.Security;

namespace Karry.Application.Auth;

/// <summary>
/// Issues a token pair (short-lived access token + rotated refresh token) for a user and
/// persists the refresh token with rotation lineage. When a family id is provided, the new
/// refresh token joins that family (refresh rotation); otherwise a new family is created
/// (fresh login). The parent (when supplied) is revoked.
/// </summary>
public interface ITokenIssuer
{
    Task<AuthTokensResponse> IssueAsync(
        Guid userId,
        Guid? tenantId,
        string name,
        string? roleCode,
        IEnumerable<string> permissions,
        string deviceId,
        TimeSpan refreshLifetime,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken,
        Guid? familyId = null,
        Guid? parentTokenId = null);
}