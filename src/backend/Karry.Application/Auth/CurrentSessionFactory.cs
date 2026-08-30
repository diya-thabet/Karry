using Karry.Application.Common;

namespace Karry.Application.Auth;

/// <summary>
/// Builds an <see cref="ICurrentSession"/> from the authenticated principal's claims.
/// Permissions are parsed from <c>permission</c> claims of the form <c>resource:action</c>.
/// </summary>
public static class CurrentSessionFactory
{
    public static ICurrentSession FromClaims(System.Security.Claims.ClaimsPrincipal principal)
    {
        var userId = Guid.TryParse(principal.FindFirst("sub")?.Value, out var uid) ? uid : (Guid?)null;
        var tenantId = Guid.TryParse(principal.FindFirst("tenant_id")?.Value, out var tid) ? tid : (Guid?)null;

        var permissions = principal
            .FindAll("permission")
            .Select(c => c.Value)
            .ToHashSet(StringComparer.Ordinal);

        return new Session(userId, tenantId, principal.FindFirst("role")?.Value, permissions);
    }

    private sealed record Session(
        Guid? UserId,
        Guid? TenantId,
        string? RoleCode,
        IReadOnlySet<string> Permissions) : ICurrentSession;
}