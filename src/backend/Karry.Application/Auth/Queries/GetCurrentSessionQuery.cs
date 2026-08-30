using Karry.Application.Common;
using Karry.Domain.Identity;
using Karry.Domain.Common;
using MediatR;

namespace Karry.Application.Auth.Queries;

public sealed record CurrentSessionResponse(
    Guid UserId,
    string Email,
    string Name,
    Guid? TenantId,
    string? RoleCode,
    bool IsPlatformAdmin,
    bool TwoFactorEnabled,
    IReadOnlyList<string> Permissions);

public sealed record GetCurrentSessionQuery() : IRequest<CurrentSessionResponse>;

public sealed class GetCurrentSessionQueryHandler : IRequestHandler<GetCurrentSessionQuery, CurrentSessionResponse>
{
    private readonly IRepository<User> _users;
    private readonly ICurrentSession _session;

    public GetCurrentSessionQueryHandler(IRepository<User> users, ICurrentSession session)
    {
        _users = users;
        _session = session;
    }

    public async Task<CurrentSessionResponse> Handle(GetCurrentSessionQuery request, CancellationToken cancellationToken)
    {
        var userId = _session.UserId
            ?? throw new AuthenticationException("Not authenticated.");

        var user = await _users.GetByIdAsync(userId, cancellationToken)
            ?? throw new AuthenticationException("Session user no longer exists.");

        return new CurrentSessionResponse(
            user.Id,
            user.Email.Value,
            user.Name,
            user.TenantId,
            _session.RoleCode,
            user.IsPlatformAdmin,
            user.TwoFactorEnabled,
            _session.Permissions.OrderBy(p => p).ToList());
    }
}
