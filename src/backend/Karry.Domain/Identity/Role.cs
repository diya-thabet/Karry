namespace Karry.Domain.Identity;

/// <summary>System role codes seeded per tenant.</summary>
public static class SystemRoles
{
    public const string Admin = "admin";
    public const string Controller = "controller";
    public const string Operator = "operator";
    public const string Weighmaster = "weighmaster";
    public const string Storekeeper = "storekeeper";
    public const string Executive = "executive";

    public static IReadOnlyCollection<string> All { get; } =
        [Admin, Controller, Operator, Weighmaster, Storekeeper, Executive];
}

/// <summary>Tenant-scoped role. A user holds exactly one role (per plan §4.1 <c>users.role_id</c>).</summary>
public sealed class Role : Common.BaseEntity, Common.IAuditableEntity, Common.ITenantScoped
{
    public Guid TenantId { get; private set; }

    /// <summary>Unique role code within the tenant (e.g. <c>operator</c>).</summary>
    public string Code { get; private set; } = default!;

    public string Name { get; private set; } = default!;

    public string? Description { get; private set; }

    /// <summary>Fixed assignable permissions (seeded from the catalog).</summary>
    private readonly List<RolePermission> _permissions = [];
    public IReadOnlyList<RolePermission> Permissions => _permissions.AsReadOnly();

    public Guid CreatedBy { get; private set; }

    public Guid? ModifiedBy { get; private set; }

    private Role()
    {
    }

    public static Role Create(
        Guid tenantId,
        string code,
        string name,
        string? description,
        IReadOnlyCollection<Permission> permissions,
        Guid createdBy)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Role code is required.", nameof(code));
        }

        var role = new Role
        {
            TenantId = tenantId,
            Code = code.Trim().ToLowerInvariant(),
            Name = string.IsNullOrWhiteSpace(name) ? code.Trim() : name.Trim(),
            Description = description,
            CreatedBy = createdBy,
        };

        foreach (var permission in permissions)
        {
            role.Grant(permission, createdBy);
        }

        return role;
    }

    public void Grant(Permission permission, Guid modifiedBy)
    {
        ArgumentNullException.ThrowIfNull(permission);

        if (_permissions.Any(p =>
                p.Resource == permission.Resource.ToLowerInvariant() && p.Action == permission.Action))
        {
            return;
        }

        _permissions.Add(new RolePermission(Id, permission.Id, permission.Resource, permission.Action));
        ModifiedBy = modifiedBy;
        MarkUpdated();
    }

    public void Revoke(Guid permissionId, Guid modifiedBy)
    {
        var match = _permissions.FirstOrDefault(p => p.PermissionId == permissionId);

        if (match is null)
        {
            return;
        }

        _permissions.Remove(match);
        ModifiedBy = modifiedBy;
        MarkUpdated();
    }

    public bool HasPermission(string resource, PermissionAction action) =>
        _permissions.Any(p => p.Resource == resource.ToLowerInvariant() && p.Action == action);

    void Common.ITenantScoped.SetTenantId(Guid tenantId) => TenantId = tenantId;
}

/// <summary>Join payload between a role and a permission; resource/action denormalized so
/// permission checks are possible without joining to the global <see cref="Permission"/> catalog.</summary>
public sealed record RolePermission(Guid RoleId, Guid PermissionId, string Resource, PermissionAction Action);