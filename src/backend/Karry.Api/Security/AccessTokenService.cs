using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Karry.Application.Security;
using Microsoft.IdentityModel.Tokens;

namespace Karry.Api.Security;

/// <summary>Issues JWT access tokens with tenant, role, and permission claims.</summary>
public sealed class AccessTokenService : IAccessTokenService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly SymmetricSecurityKey _key;
    private readonly int _expiryMinutes;

    public AccessTokenService(IConfiguration configuration)
    {
        _issuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
        _audience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured.");
        var secret = configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        _expiryMinutes = configuration.GetValue<int?>("Jwt:ExpiryMinutes") ?? 15;
    }

    public string CreateAccessToken(
        Guid userId,
        Guid? tenantId,
        string name,
        string roleCode,
        IEnumerable<string> permissions,
        string deviceId)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        if (tenantId is not null)
        {
            claims.Add(new Claim("tenant_id", tenantId.Value.ToString()));
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            claims.Add(new Claim("name", name));
        }

        if (!string.IsNullOrWhiteSpace(roleCode))
        {
            claims.Add(new Claim("role", roleCode));
        }

        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            claims.Add(new Claim("device_id", deviceId));
        }

        foreach (var permission in permissions.Distinct(StringComparer.Ordinal))
        {
            claims.Add(new Claim("permission", permission));
        }

        var credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(_expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}