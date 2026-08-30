using System.Security.Cryptography;
using System.Text;
using Karry.Application.Auth;
using Karry.Application.Common;
using Karry.Application.Security;

namespace Karry.Tests.Support;

public sealed class FakeClock : IClock
{
    public FakeClock(DateTime? utcNow = null) => UtcNow = utcNow ?? DateTime.UtcNow;
    public DateTime UtcNow { get; set; }
}

public sealed class FakeSession : ICurrentSession
{
    public Guid? UserId { get; init; }
    public Guid? TenantId { get; init; }
    public string? RoleCode { get; init; }
    public IReadOnlySet<string> Permissions { get; init; } = new HashSet<string>();

    public static FakeSession Admin(Guid tenantId, Guid userId) =>
        new() { UserId = userId, TenantId = tenantId, RoleCode = "admin" };

    public static FakeSession Operator(Guid tenantId, Guid userId, IReadOnlySet<string>? permissions = null) =>
        new() { UserId = userId, TenantId = tenantId, RoleCode = "operator", Permissions = permissions ?? new HashSet<string>() };
}

public sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed:{password}";

    public bool Verify(string password, string passwordHash) => passwordHash == $"hashed:{password}";
}

public sealed class FakeSecureRandom : ISecureRandom
{
    private int _counter;
    public string RandomHex(int byteLength) => $"tok{Interlocked.Increment(ref _counter):D4}".PadLeft(32, '0');
}

public class FakeTotp : ITotpService
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public string GenerateSecret() => "SECRETKEY";

    public string BuildProvisioningUri(string issuer, string accountName, string secret)
        => $"otpauth://totp/{issuer}:{accountName}?secret={secret}&issuer={issuer}";

    public virtual bool Validate(string secret, string code, TimeSpan clockSkew) => code == "123456";

    public string GenerateCode(string secret, DateTime utcNow) => "123456";
}

public sealed class FakeAccessTokenService : IAccessTokenService
{
    public string CreateAccessToken(
        Guid userId, Guid? tenantId, string name, string roleCode, IEnumerable<string> permissions, string deviceId)
        => $"access.{userId:N}.{tenantId?.ToString("N") ?? "platform"}";
}

public sealed class FakeTokenIssuer : ITokenIssuer
{
    private readonly InMemoryRepository<Karry.Domain.Identity.RefreshToken> _tokens;

    public FakeTokenIssuer(InMemoryRepository<Karry.Domain.Identity.RefreshToken>? tokens = null)
    {
        _tokens = tokens ?? new InMemoryRepository<Karry.Domain.Identity.RefreshToken>();
    }

    public InMemoryRepository<Karry.Domain.Identity.RefreshToken> Tokens => _tokens;

    public async Task<AuthTokensResponse> IssueAsync(
        Guid userId, Guid? tenantId, string name, string? roleCode, IEnumerable<string> permissions,
        string deviceId, TimeSpan refreshLifetime, string? ipAddress, string? userAgent,
        CancellationToken cancellationToken, Guid? familyId = null, Guid? parentTokenId = null)
    {
        var raw = $"refresh.{Guid.NewGuid():N}";
        var token = Karry.Domain.Identity.RefreshToken.Create(
            userId, Karry.Application.Security.RefreshTokenHasher.Hash(raw), familyId ?? Guid.Empty,
            deviceId, DateTime.UtcNow.Add(refreshLifetime), ipAddress, userAgent);
        await _tokens.AddAsync(token, cancellationToken);

        if (parentTokenId is not null)
        {
            var parent = await _tokens.GetByIdAsync(parentTokenId.Value, cancellationToken);
            parent?.Revoke(token.Id, DateTime.UtcNow);
        }

        return new AuthTokensResponse($"access.{userId:N}", raw, token.Id);
    }
}