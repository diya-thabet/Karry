using System.Security.Cryptography;
using System.Text;
using Karry.Application.Common;

namespace Karry.Application.Security;

/// <summary>Hashes refresh-token values (SHA-256) so only digests are ever persisted.</summary>
public static class RefreshTokenHasher
{
    public static string Hash(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

/// <summary>Default cryptographically-secure random provider.</summary>
public sealed class SecureRandom : ISecureRandom
{
    public string RandomHex(int byteLength) => Convert.ToHexString(RandomNumberGenerator.GetBytes(byteLength)).ToLowerInvariant();
}