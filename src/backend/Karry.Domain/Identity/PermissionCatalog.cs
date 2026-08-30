namespace Karry.Domain.Identity;

/// <summary>
/// Canonical resources used across the platform. Kept as constants to avoid string spread.
/// </summary>
public static class Resources
{
    public const string Units = "units";
    public const string Tenants = "tenants";
    public const string Users = "users";
    public const string Roles = "roles";
    public const string Machines = "machines";
    public const string WearParts = "wear_parts";
    public const string Shifts = "shifts";
    public const string ScaleTickets = "scale_tickets";
    public const string Warehouse = "warehouse";
    public const string Ledger = "ledger";
    public const string Audit = "audit";
    public const string Maintenance = "maintenance";
}

/// <summary>
/// The system authorization matrix: which (resource, action) each of the six roles is granted.
/// This is the single source of truth used by seeding and by authorization checks.
/// <para>
///   - <see cref="PermissionAction.Read"/>  — visible unmasked
///   - <see cref="PermissionAction.Write"/> — may create/update
///   - <see cref="PermissionAction.Mask"/>  — read-only, sensitive fields masked
/// </para>
/// </summary>
public static class PermissionCatalog
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, PermissionAction[]>> Matrix =
        new Dictionary<string, IReadOnlyDictionary<string, PermissionAction[]>>
        {
            [SystemRoles.Admin] = new Dictionary<string, PermissionAction[]>
            {
                [Resources.Units] = [PermissionAction.Read, PermissionAction.Write],
                [Resources.Tenants] = [PermissionAction.Read, PermissionAction.Write],
                [Resources.Users] = [PermissionAction.Read, PermissionAction.Write],
                [Resources.Roles] = [PermissionAction.Read, PermissionAction.Write],
                [Resources.Machines] = [PermissionAction.Read, PermissionAction.Write],
                [Resources.WearParts] = [PermissionAction.Read, PermissionAction.Write],
                [Resources.Shifts] = [PermissionAction.Read, PermissionAction.Write],
                [Resources.ScaleTickets] = [PermissionAction.Read, PermissionAction.Write],
                [Resources.Warehouse] = [PermissionAction.Read, PermissionAction.Write],
                [Resources.Ledger] = [PermissionAction.Read, PermissionAction.Write],
                [Resources.Audit] = [PermissionAction.Read],
                [Resources.Maintenance] = [PermissionAction.Read, PermissionAction.Write],
            },
            [SystemRoles.Executive] = new Dictionary<string, PermissionAction[]>
            {
                [Resources.Ledger] = [PermissionAction.Read],
                [Resources.Units] = [PermissionAction.Read],
                [Resources.Users] = [PermissionAction.Mask],
                [Resources.Machines] = [PermissionAction.Read],
                [Resources.Shifts] = [PermissionAction.Read],
                [Resources.ScaleTickets] = [PermissionAction.Read],
                [Resources.Warehouse] = [PermissionAction.Read],
                [Resources.Maintenance] = [PermissionAction.Read],
                [Resources.Audit] = [PermissionAction.Read],
            },
            [SystemRoles.Controller] = new Dictionary<string, PermissionAction[]>
            {
                [Resources.Shifts] = [PermissionAction.Read, PermissionAction.Write],
                [Resources.ScaleTickets] = [PermissionAction.Read],
                [Resources.Machines] = [PermissionAction.Read],
                [Resources.Units] = [PermissionAction.Read],
                [Resources.Users] = [PermissionAction.Mask],
                [Resources.Maintenance] = [PermissionAction.Read],
            },
            [SystemRoles.Operator] = new Dictionary<string, PermissionAction[]>
            {
                [Resources.Shifts] = [PermissionAction.Write],
                [Resources.Machines] = [PermissionAction.Read],
                [Resources.Units] = [PermissionAction.Read],
                [Resources.WearParts] = [PermissionAction.Mask],
            },
            [SystemRoles.Weighmaster] = new Dictionary<string, PermissionAction[]>
            {
                [Resources.ScaleTickets] = [PermissionAction.Read, PermissionAction.Write],
                [Resources.Units] = [PermissionAction.Read],
                [Resources.Machines] = [PermissionAction.Read],
            },
            [SystemRoles.Storekeeper] = new Dictionary<string, PermissionAction[]>
            {
                [Resources.Warehouse] = [PermissionAction.Read, PermissionAction.Write],
                [Resources.Units] = [PermissionAction.Read],
                [Resources.Machines] = [PermissionAction.Read],
            },
        };

    /// <summary>Resources/actions granted to a system role.</summary>
    public static IReadOnlyDictionary<string, PermissionAction[]> ForRole(string roleCode)
        => Matrix.TryGetValue(roleCode, out var grants) ? grants : new Dictionary<string, PermissionAction[]>();

    public static bool HasGrant(string roleCode, string resource, PermissionAction action)
        => ForRole(roleCode).TryGetValue(resource, out var actions) && actions.Contains(action);

    public static IEnumerable<string> RoleCodes => Matrix.Keys;

    /// <summary>All distinct (resource, action) pairs appearing in the matrix — used to seed the global catalog.</summary>
    public static IEnumerable<(string Resource, PermissionAction Action, string RoleCode)> Flatten()
    {
        foreach (var (role, grants) in Matrix)
        {
            foreach (var (resource, actions) in grants)
            {
                foreach (var action in actions)
                {
                    yield return (resource, action, role);
                }
            }
        }
    }
}