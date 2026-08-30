namespace Karry.IntegrationTests;

public sealed class SkipException : Exception
{
    public SkipException(string message) : base(message)
    {
    }
}

public record LoginRequest(string Email, string Password, string DeviceId);

public record LoginResponse(bool RequiresTwoFactor, string? ChallengeToken, TokenPair? Tokens, Guid? UserId, string? RoleCode, string? TwoFactorProvisioningUri);

public record TokenPair(string AccessToken, string RefreshToken, Guid RefreshTokenId);

public record CreateTenantResponse(Guid TenantId, string Name);

public record RoleResponse(Guid RoleId, string Code, string Name, string? Description, IReadOnlyList<string> Permissions);

public record CreateUserResponse(Guid UserId, string Email);

public record UserResponse(Guid UserId, string Email, string Name, bool IsActive, bool TwoFactorEnabled, Guid? RoleId, DateTime CreatedAtUtc, string? RoleCode);

public record ConvertResponse(decimal Value, string ToUnit);