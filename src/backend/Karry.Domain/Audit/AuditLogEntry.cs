namespace Karry.Domain.Audit;

/// <summary>Write outcome of an audited action.</summary>
public enum AuditOutcome
{
    Succeeded = 0,
    Failed = 1,
}

/// <summary>
/// Append-only audit record. Rows are never updated or deleted — this mirrors the plan's
/// <c>audit_log</c> (id, tenant_id, user_id, action, entity_type, entity_id, before, after, timestamp).
/// </summary>
public sealed class AuditLogEntry : Common.BaseEntity, Common.ITenantScoped
{
    public Guid TenantId { get; private set; }

    /// <summary>Acting user; null for platform/system events.</summary>
    public Guid? UserId { get; private set; }

    public string Action { get; private set; } = default!;

    public string? EntityType { get; private set; }

    public string? EntityId { get; private set; }

    public string? Before { get; private set; }

    public string? After { get; private set; }

    public string? IpAddress { get; private set; }

    public string? DeviceId { get; private set; }

    public AuditOutcome Outcome { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    private AuditLogEntry()
    {
    }

    public static AuditLogEntry Create(
        Guid? tenantId,
        Guid? userId,
        string action,
        string? entityType,
        string? entityId,
        string? before,
        string? after,
        AuditOutcome outcome,
        string? ipAddress = null,
        string? deviceId = null)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Audit action is required.", nameof(action));
        }

        return new AuditLogEntry
        {
            TenantId = tenantId ?? throw new ArgumentException("Tenant is required.", nameof(tenantId)),
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Before = before,
            After = after,
            Outcome = outcome,
            IpAddress = ipAddress,
            DeviceId = deviceId,
            OccurredAtUtc = DateTime.UtcNow,
        };
    }

    void Common.ITenantScoped.SetTenantId(Guid tenantId) => TenantId = tenantId;
}