namespace Karry.Application.Common;

/// <summary>Thrown when the requested resource does not exist. Maps to 404.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}

/// <summary>Thrown when credentials are invalid or a session is not trusted. Maps to 401.</summary>
public sealed class AuthenticationException : Exception
{
    public AuthenticationException(string message) : base(message)
    {
    }
}

/// <summary>Thrown when the caller lacks a required permission. Maps to 403.</summary>
public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }
}

/// <summary>Thrown when an operation conflicts with existing state (duplicates, state transitions). Maps to 409.</summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}

/// <summary>Thrown for an already-locked-out account or other lockout-related transition. Maps to 423 (Locked).</summary>
public sealed class AccountLockedException : Exception
{
    public AccountLockedException(string message) : base(message)
    {
    }
}