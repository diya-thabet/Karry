using Karry.Application.Common;
using Karry.Application.Security;
using Karry.Domain.Audit;
using Karry.Domain.Identity;
using Karry.Domain.Common;
using Karry.Domain.Tenants;
using MediatR;

namespace Karry.Application.Auth.Commands;

public sealed record RefreshCommand(RefreshTokenRequest Input) : IRequest<AuthTokensResponse>;

public sealed class RefreshCommandHandler : IRequestHandler<RefreshCommand, AuthTokensResponse>
{
    private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(30);

    private readonly IRepository<RefreshToken> _tokens;
    private readonly IRepository<User> _users;
    private readonly IRepository<Role> _roles;
    private readonly IRepository<AuditLogEntry> _audit;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenIssuer _tokenIssuer;
    private readonly IClock _clock;

    public RefreshCommandHandler(
        IRepository<RefreshToken> tokens,
        IRepository<User> users,
        IRepository<Role> roles,
        IRepository<AuditLogEntry> audit,
        IUnitOfWork unitOfWork,
        ITokenIssuer tokenIssuer,
        IClock clock)
    {
        _tokens = tokens;
        _users = users;
        _roles = roles;
        _audit = audit;
        _unitOfWork = unitOfWork;
        _tokenIssuer = tokenIssuer;
        _clock = clock;
    }

    public async Task<AuthTokensResponse> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        var input = request.Input;
        var hash = RefreshTokenHasher.Hash(input.RefreshToken);
        var stored = await _tokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken)
            ?? throw new AuthenticationException("Invalid refresh token.");

        var now = _clock.UtcNow;
        var status = stored.StatusAt(now);

        if (status == RefreshTokenStatus.Revoked)
        {
            // Reuse of a revoked token: revoke the entire family (replay detection).
            var familyTokens = await _tokens.ListAsync(t => t.FamilyId == stored.FamilyId, cancellationToken);
            foreach (var familyToken in familyTokens)
            {
                familyToken.RevokeFamilyEntry(now);
            }

            await WriteAuditAsync(stored, "refresh.reuse.revoked", input.IpAddress, input.DeviceId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw new AuthenticationException("Refresh token has been revoked. Please sign in again.");
        }

        if (status == RefreshTokenStatus.Expired)
        {
            throw new AuthenticationException("Refresh token has expired. Please sign in again.");
        }

        var user = await _users.GetByIdAsync(stored.UserId, cancellationToken)
            ?? throw new AuthenticationException("User no longer exists.");

        if (!user.IsActive)
        {
            throw new AuthenticationException("Account is not active.");
        }

        var (roleCode, permissions) = await BuildPrincipalAsync(user, cancellationToken);

        // Issue a child token within the same family; the issuer revokes the parent.
        var tokens = await _tokenIssuer.IssueAsync(
            user.Id, user.TenantId, user.Name, roleCode, permissions,
            input.DeviceId, RefreshLifetime, input.IpAddress, input.UserAgent, cancellationToken,
            familyId: stored.FamilyId, parentTokenId: stored.Id);

        await WriteAuditAsync(stored, "refresh.succeeded", input.IpAddress, input.DeviceId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return tokens;
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
        RefreshToken token, string action, string? ip, string? deviceId, CancellationToken cancellationToken)
    {
        await _audit.AddAsync(
            AuditLogEntry.Create(
                Guid.Empty,
                token.UserId,
                action,
                "refresh_token",
                token.Id.ToString(),
                before: null,
                after: null,
                AuditOutcome.Succeeded,
                ip,
                deviceId),
            cancellationToken);
    }
}