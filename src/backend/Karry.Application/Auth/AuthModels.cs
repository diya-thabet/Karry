namespace Karry.Application.Auth;

public sealed record LoginRequest(string Email, string Password, string DeviceId, string? IpAddress = null, string? UserAgent = null);

public sealed record TwoFactorChallengeRequest(string Email, string Code, string DeviceId, string? IpAddress = null, string? UserAgent = null);

public sealed record RefreshTokenRequest(string RefreshToken, string DeviceId, string? IpAddress = null, string? UserAgent = null);

public sealed record LogoutRequest(string RefreshToken);

public sealed record AuthTokensResponse(string AccessToken, string RefreshToken, Guid RefreshTokenId);

public sealed record LoginResponse(
    bool RequiresTwoFactor,
    string? ChallengeToken,
    AuthTokensResponse? Tokens,
    Guid? UserId,
    string? RoleCode,
    string? TwoFactorProvisioningUri);

public sealed record RegisterRequest(
    string Email,
    string Name,
    string Password,
    Guid RoleId,
    string? DeviceId = null,
    string? IpAddress = null,
    string? UserAgent = null);