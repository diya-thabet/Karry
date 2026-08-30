using Karry.Application.Common;
using Microsoft.AspNetCore.Http;

namespace Karry.Infrastructure.Context;

/// <summary>
/// Resolves the current <see cref="ICurrentSession"/> (user, tenant, role, permissions) from
/// the authenticated principal's claims. Permissions are read from <c>permission</c> claims.
/// </summary>
public sealed class CurrentSessionProvider : ICurrentSession
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentSessionProvider(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid? UserId
    {
        get
        {
            var value = _accessor.HttpContext?.User.FindFirst("sub")?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public Guid? TenantId
    {
        get
        {
            var value = _accessor.HttpContext?.User.FindFirst("tenant_id")?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? RoleCode => _accessor.HttpContext?.User.FindFirst("role")?.Value;

    public IReadOnlySet<string> Permissions
        => _accessor.HttpContext?.User
            .FindAll("permission")
            .Select(c => c.Value)
            .ToHashSet(StringComparer.Ordinal) ?? new HashSet<string>(StringComparer.Ordinal);
}