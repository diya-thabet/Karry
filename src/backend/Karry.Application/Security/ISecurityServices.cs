namespace Karry.Application.Security;

/// <summary>Port for password hashing (implemented with ASP.NET Identity's PasswordHasher).</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}

/// <summary>Port for TOTP (RFC 6238) two-factor codes and secrets.</summary>
public interface ITotpService
{
    /// <summary>Generates a new random base32 secret (20 bytes, SHA-1 TOTP).</summary>
    string GenerateSecret();

    /// <summary>Builds an otpauth:// URI for QR enrollment.</summary>
    string BuildProvisioningUri(string issuer, string accountName, string secret);

    /// <summary>Validates a 6-digit code against the secret within ±window steps.</summary>
    bool Validate(string secret, string code, TimeSpan clockSkew);

    /// <summary>Computes the current-time expected code (tests + verification).</summary>
    string GenerateCode(string secret, DateTime utcNow);
}

/// <summary>Port for issuing short-lived JWT access tokens.</summary>
public interface IAccessTokenService
{
    string CreateAccessToken(Guid userId, Guid? tenantId, string name, string roleCode, IEnumerable<string> permissions, string deviceId);
}