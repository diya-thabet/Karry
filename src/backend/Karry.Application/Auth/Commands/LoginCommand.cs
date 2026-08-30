using Karry.Application.Common;
using Karry.Application.Security;
using Karry.Domain.Audit;
using Karry.Domain.Identity;
using Karry.Domain.Common;
using Karry.Domain.Tenants;
using MediatR;

namespace Karry.Application.Auth.Commands;

public sealed record LoginCommand(LoginRequest Input) : IRequest<LoginResponse>;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(30);

    private readonly IRepository<User> _users;
    private readonly IRepository<Role> _roles;
    private readonly IRepository<Tenant> _tenants;
    private readonly IRepository<AuditLogEntry> _audit;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenIssuer _tokenIssuer;
    private readonly IClock _clock;

    public LoginCommandHandler(
        IRepository<User> users,
        IRepository<Role> roles,
        IRepository<Tenant> tenants,
        IRepository<AuditLogEntry> audit,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenIssuer tokenIssuer,
        IClock clock)
    {
        _users = users;
        _roles = roles;
        _tenants = tenants;
        _audit = audit;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenIssuer = tokenIssuer;
        _clock = clock;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var input = request.Input;

        // Admins may also log in via the global (tenant_id NULL) user row.
        var user = await _users.FirstOrDefaultAsync(u => u.Email.Value == EmailAddress.Create(input.Email).Value, cancellationToken)
            ?? throw new AuthenticationException("Invalid email or password.");

        var now = _clock.UtcNow;

        if (!user.IsActive)
        {
            await WriteAuditAsync(user, "login.failed.inactive", input.IpAddress, input.DeviceId, cancellationToken);
            throw new AuthenticationException("Account is not active.");
        }

        var guard = user.EvaluateLoginGuard(now);

        if (guard.LockedOut)
        {
            await WriteAuditAsync(user, "login.failed.locked", input.IpAddress, input.DeviceId, cancellationToken);
            throw new AccountLockedException("Account is temporarily locked due to repeated failed attempts.");
        }

        if (!_passwordHasher.Verify(input.Password, user.PasswordHash))
        {
            user.RecordFailedLogin(now);
            _users.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await WriteAuditAsync(user, "login.failed.password", input.IpAddress, input.DeviceId, cancellationToken);

            if (user.FailedLoginCount >= User.MaxFailedAttempts)
            {
                throw new AccountLockedException("Account is temporarily locked due to repeated failed attempts.");
            }

            var remaining = Math.Max(0, User.MaxFailedAttempts - user.FailedLoginCount);
            throw new AuthenticationException($"Invalid email or password. {remaining} attempt(s) remaining.");
        }

        if (user.TwoFactorEnabled)
        {
            return new LoginResponse(
                RequiresTwoFactor: true,
                ChallengeToken: "2fa-required-" + Guid.NewGuid(),
                Tokens: null,
                UserId: user.Id,
                RoleCode: await GetRoleCodeAsync(user, cancellationToken),
                TwoFactorProvisioningUri: null);
        }

        var (roleCode, permissions) = await BuildPrincipalAsync(user, cancellationToken);
        user.RecordLogin(input.DeviceId, now, user.Id);
        _users.Update(user);

        var tokens = await _tokenIssuer.IssueAsync(
            user.Id, user.TenantId, user.Name, roleCode, permissions,
            input.DeviceId, RefreshLifetime, input.IpAddress, input.UserAgent, cancellationToken);

        await WriteAuditAsync(user, "login.succeeded", input.IpAddress, input.DeviceId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResponse(false, null, tokens, user.Id, roleCode, null);
    }

    private async Task<(string? RoleCode, IEnumerable<string> Permissions)> BuildPrincipalAsync(
        User user, CancellationToken cancellationToken)
    {
        if (user.IsPlatformAdmin)
        {
            return (null, PermissionCatalog.Flatten().Select(p => $"{p.Resource}:{p.Action}").Distinct());
        }

        if (user.RoleId is null)
        {
            return (null, []);
        }

        var role = await _roles.GetByIdAsync(user.RoleId.Value, cancellationToken);
        if (role is null)
        {
            return (null, []);
        }

        return (role.Code, role.Permissions.Select(p => $"{p.Resource}:{p.Action}"));
    }

    private async Task<string?> GetRoleCodeAsync(User user, CancellationToken cancellationToken)
    {
        if (user.IsPlatformAdmin || user.RoleId is null)
        {
            return null;
        }

        var role = await _roles.GetByIdAsync(user.RoleId.Value, cancellationToken);
        return role?.Code;
    }

    private async Task WriteAuditAsync(
        User user, string action, string? ip, string? deviceId, CancellationToken cancellationToken)
    {
        await _audit.AddAsync(
            AuditLogEntry.Create(
                user.TenantId ?? Guid.Empty,
                user.Id,
                action,
                "user",
                user.Id.ToString(),
                before: null,
                after: null,
                AuditOutcome.Succeeded,
                ip,
                deviceId),
            cancellationToken);
    }
}