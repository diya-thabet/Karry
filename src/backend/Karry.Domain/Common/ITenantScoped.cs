namespace Karry.Domain.Common;

/// <summary>
/// Implemented by entities owned by a tenant so the persistence layer can stamp the
/// current tenant id on newly-added rows.
/// </summary>
public interface ITenantScoped
{
    void SetTenantId(Guid tenantId);
}