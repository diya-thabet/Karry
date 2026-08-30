using Karry.Application.Auth;
using Karry.Application.Common;
using Karry.Application.Security;
using Karry.Domain.Audit;
using Karry.Domain.Identity;
using Karry.Domain.Common;
using MediatR;

namespace Karry.Application.Auth.Commands;

public sealed record TwoFactorLoginCommand(TwoFactorChallengeRequest Input) : IRequest<LoginResponse>;

public sealed class TwoFactorLoginCommandHandler : IRequestHandler<TwoFactorLoginCommand, LoginResponse>
{
    private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(30);
    private static readonly TimeSpan TotpClockSkew = TimeSpan.FromSeconds(30);

    private readonly IRepository<User> _users;
    private readonly IRepository<Role> _roles;
    private readonly IRepository<AuditLogEntry> _audit;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITotpService _totp;
    private readonly ITokenIssuer _tokenIssuer;
    private readonly IClock _clock;

    public TwoFactorLoginCommandHandler(
        IRepository<User> users,
        IRepository<Role> roles,
        IRepository<AuditLogEntry> audit,
        IUnitOfWork unitOfWork,
        ITotpService totp,
        ITokenIssuer tokenIssuer,
        IClock clock)
    {
        _users = users;
        _roles = roles;
        _audit = audit;
        _unitOfWork = unitOfWork;
        _totp = totp;
        _tokenIssuer = tokenIssuer;
        _clock = clock;
    }

    public async Task<LoginResponse> Handle(TwoFactorLoginCommand request, CancellationToken cancellationToken)
    {
        var input = request.Input;
        var user = await _users.FirstOrDefaultAsync(u => u.Email.Value == EmailAddress.Create(input.Email).Value, cancellationToken)
            ?? throw new AuthenticationException("Invalid credentials.");

        if (!user.IsActive)
        {
            throw new AuthenticationException("Account is not active.");
        }

        if (!user.TwoFactorEnabled || string.IsNullOrEmpty(user.TotpSecret))
        {
            throw new AuthenticationException("Two-factor authentication is not enabled for this account.");
        }

        var guard = user.EvaluateLoginGuard(_clock.UtcNow);
        if (guard.LockedOut)
        {
            throw new AccountLockedException("Account is temporarily locked due to repeated failed attempts.");
        }

        if (!_totp.Validate(user.TotpSecret, input.Code, TotpClockSkew))
        {
            var now = _clock.UtcNow;
            user.RecordFailedLogin(now);
            _users.Update(user);
            await WriteAuditAsync(user, "login.2fa.failed", input.IpAddress, input.DeviceId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new AuthenticationException("Invalid or expired two-factor code.");
        }

        var (roleCode, permissions) = await BuildPrincipalAsync(user, cancellationToken);
        var nowUtc = _clock.UtcNow;
        user.RecordLogin(input.DeviceId, nowUtc, user.Id);
        _users.Update(user);

        var tokens = await _tokenIssuer.IssueAsync(
            user.Id, user.TenantId, user.Name, roleCode, permissions,
            input.DeviceId, RefreshLifetime, input.IpAddress, input.UserAgent, cancellationToken);

        await WriteAuditAsync(user, "login.2fa.succeeded", input.IpAddress, input.DeviceId, cancellationToken);
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
        return (role?.Code, role?.Permissions.Select(p => $"{p.Resource}:{p.Action}") ?? []);
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