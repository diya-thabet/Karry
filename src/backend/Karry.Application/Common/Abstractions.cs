namespace Karry.Application.Common;

/// <summary>Abstraction over wall-clock time for deterministic testing.</summary>
public interface IClock
{
    DateTime UtcNow { get; }
}

/// <summary>Abstraction over cryptographically-secure randomness used for tokens/secrets.</summary>
public interface ISecureRandom
{
    string RandomHex(int byteLength);
}

/// <summary>Current request session: who is calling and with what permissions.</summary>
public interface ICurrentSession
{
    Guid? UserId { get; }

    Guid? TenantId { get; }

    string? RoleCode { get; }

    /// <summary>Permission claims formatted as <c>resource:action</c>, e.g. <c>users:write</c>.</summary>
    IReadOnlySet<string> Permissions { get; }
}