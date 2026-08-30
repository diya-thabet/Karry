using Karry.Domain.Identity;

namespace Karry.Application.Security;

/// <summary>Visibility of a field for a given role.</summary>
public enum FieldVisibility
{
    /// <summary>Visible verbatim (role has Read).</summary>
    Visible = 0,

    /// <summary>Visible but masked (role has Mask only).</summary>
    Masked = 1,

    /// <summary>Hidden entirely (no grant).</summary>
    Hidden = 2,
}

/// <summary>
/// Decision engine for field-level masking. Uses the canonical <see cref="PermissionCatalog"/>:
/// Read ⇒ unmasked, Mask ⇒ masked, neither ⇒ hidden.
/// </summary>
public sealed class FieldMaskingEvaluator
{
    public FieldVisibility Evaluate(string roleCode, string resource)
    {
        // A role that can read or write a resource sees its fields unmasked (write implies read).
        if (PermissionCatalog.HasGrant(roleCode, resource, PermissionAction.Read)
            || PermissionCatalog.HasGrant(roleCode, resource, PermissionAction.Write))
        {
            return FieldVisibility.Visible;
        }

        if (PermissionCatalog.HasGrant(roleCode, resource, PermissionAction.Mask))
        {
            return FieldVisibility.Masked;
        }

        return FieldVisibility.Hidden;
    }

    /// <summary>Masks a value (used when the evaluator decides Masked).</summary>
    public static string Mask(string? value) => string.IsNullOrEmpty(value) ? "••••••" : "••••••";
}