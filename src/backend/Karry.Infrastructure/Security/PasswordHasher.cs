using Karry.Application.Security;
using Microsoft.AspNetCore.Identity;

namespace Karry.Infrastructure.Security;

/// <summary>ASP.NET Identity PBKDF2 password hasher (with per-user random salt).</summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string password, string passwordHash)
        => _hasher.VerifyHashedPassword(null!, passwordHash, password) != PasswordVerificationResult.Failed;
}