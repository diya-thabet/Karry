namespace Karry.Application.Users;

public sealed record CreateUserRequest(string Email, string Name, string Password, Guid RoleId);

public sealed record CreateUserResponse(Guid UserId, string Email);

public sealed record UserResponse(Guid UserId, string Email, string Name, bool IsActive, bool TwoFactorEnabled, Guid? RoleId, DateTime CreatedAtUtc, string? RoleCode);