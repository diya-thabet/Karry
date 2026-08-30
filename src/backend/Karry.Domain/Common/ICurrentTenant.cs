using Karry.Domain.Common;

namespace Karry.Domain.Common;

/// <summary>
/// Resolves the active tenant id bound to the current HTTP request context.
/// Set once per request by the Api middleware; consumed by repositories and services.
/// </summary>
public interface ICurrentTenant
{
    Guid? TenantId { get; }
}

/// <summary>
/// Resolves the id of the authenticated user for audit attribution.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
}