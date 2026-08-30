namespace Karry.Domain.Identity;

/// <summary>Action a permission grants against a resource.</summary>
public enum PermissionAction
{
    Read = 0,
    Write = 1,
    Mask = 2,
}

/// <summary>
/// A coarse-grained capability: <see cref="Resource"/> + <see cref="Action"/>.
/// <c>Mask</c> grants read-with-field-masking (e.g., operators see cost margins masked).
/// </summary>
public sealed class Permission : Common.BaseEntity
{
    public string Resource { get; private init; } = default!;

    public PermissionAction Action { get; private init; }

    public string? Description { get; private init; }

    private Permission()
    {
    }

    public static Permission Create(string resource, PermissionAction action, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(resource))
        {
            throw new ArgumentException("Resource is required.", nameof(resource));
        }

        return new Permission
        {
            Resource = resource.Trim().ToLowerInvariant(),
            Action = action,
            Description = description,
        };
    }
}