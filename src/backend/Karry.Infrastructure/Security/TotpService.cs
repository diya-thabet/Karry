using System.Net;
using System.Security.Cryptography;
using System.Text;
using Karry.Application.Security;

namespace Karry.Infrastructure.Security;

/// <summary>
/// RFC 6238 TOTP implementation (HMAC-SHA1, 6 digits, 30-second period) with a base32 secret
/// compatible with standard authenticator apps (Google/Microsoft Authenticator, Authy).
/// </summary>
public sealed class TotpService : ITotpService
{
    private const int StepSeconds = 30;
    private const int CodeLength = 6;
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    private const string Issuer = "Karry";

    public string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(20);
        return Base32Encode(bytes);
    }

    public string BuildProvisioningUri(string issuer, string accountName, string secret)
    {
        var label = $"{issuer}:{accountName}";
        var query = $"?secret={secret}&issuer={issuer}&algorithm=SHA1&digits=6&period={StepSeconds}";
        return $"otpauth://totp/{Uri.EscapeDataString(label)}{query}";
    }

    public bool Validate(string secret, string code, TimeSpan clockSkew)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != CodeLength)
        {
            return false;
        }

        var secretBytes = Base32Decode(secret);
        var windowSteps = (int)Math.Ceiling(clockSkew.TotalSeconds / StepSeconds);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        for (var offset = -windowSteps; offset <= windowSteps; offset++)
        {
            var counter = now / StepSeconds + offset;
            if (ConstantTimeEquals(ComputeCode(secretBytes, counter), code))
            {
                return true;
            }
        }

        return false;
    }

    public string GenerateCode(string secret, DateTime utcNow)
    {
        var secretBytes = Base32Decode(secret);
        var unix = (long)(utcNow.ToUniversalTime() - DateTime.UnixEpoch).TotalSeconds;
        return ComputeCode(secretBytes, unix / StepSeconds);
    }

    private static string ComputeCode(byte[] secretBytes, long counter)
    {
        using var hmac = new HMACSHA1(secretBytes);
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counterBytes);
        }

        var hash = hmac.ComputeHash(counterBytes);
        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24)
                     | ((hash[offset + 1] & 0xFF) << 16)
                     | ((hash[offset + 2] & 0xFF) << 8)
                     | (hash[offset + 3] & 0xFF);

        return (binary % 1_000_000).ToString("D6");
    }

    private static bool ConstantTimeEquals(string a, string b)
    {
        var aa = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        return CryptographicOperations.FixedTimeEquals(aa, bb);
    }

    private static string Base32Encode(byte[] data)
    {
        var builder = new StringBuilder();
        var bits = 0;
        var value = 0;

        foreach (var b in data)
        {
            value = (value << 8) | b;
            bits += 8;

            while (bits >= 5)
            {
                builder.Append(Alphabet[(value >> (bits - 5)) & 31]);
                bits -= 5;
            }
        }

        if (bits > 0)
        {
            builder.Append(Alphabet[(value << (5 - bits)) & 31]);
        }

        return builder.ToString();
    }

    private static byte[] Base32Decode(string input)
    {
        var clean = input.TrimEnd('=').ToUpperInvariant();
        var output = new List<byte>();
        var bits = 0;
        var value = 0;

        foreach (var c in clean)
        {
            if (c == ' ')
            {
                continue;
            }

            var index = Alphabet.IndexOf(c);
            if (index < 0)
            {
                throw new FormatException($"Invalid base32 character '{c}'.");
            }

            value = (value << 5) | index;
            bits += 5;

            if (bits >= 8)
            {
                output.Add((byte)((value >> (bits - 8)) & 0xFF));
                bits -= 8;
            }
        }

        return [.. output];
    }

    public string IssuerName => Issuer;
}