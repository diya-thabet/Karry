using Karry.Domain.Common;
using Microsoft.AspNetCore.Http;

namespace Karry.Infrastructure.Context;

/// <summary>
/// Resolves the current tenant and user from the ambient HTTP context. When no user is
/// authenticated, <see cref="TenantId"/> and <see cref="UserId"/> return null.
/// </summary>
public sealed class TenantContext : ICurrentTenant, ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public TenantContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid? TenantId
    {
        get
        {
            var value = _accessor.HttpContext?.User.FindFirst("tenant_id")?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public Guid? UserId
    {
        get
        {
            var value = _accessor.HttpContext?.User.FindFirst("sub")?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }
}