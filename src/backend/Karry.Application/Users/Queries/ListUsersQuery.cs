using Karry.Application.Common;
using Karry.Domain.Identity;
using Karry.Domain.Common;
using MediatR;

namespace Karry.Application.Users.Queries;

public sealed record ListUsersQuery() : IRequest<IReadOnlyList<UserResponse>>;

public sealed class ListUsersQueryHandler : IRequestHandler<ListUsersQuery, IReadOnlyList<UserResponse>>
{
    private readonly IRepository<User> _users;
    private readonly IRepository<Role> _roles;
    private readonly ICurrentSession _session;

    public ListUsersQueryHandler(IRepository<User> users, IRepository<Role> roles, ICurrentSession session)
    {
        _users = users;
        _roles = roles;
        _session = session;
    }

    public async Task<IReadOnlyList<UserResponse>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _session.TenantId
            ?? throw new ForbiddenException("Users are tenant-scoped.");
        var roleIds = new List<Guid>();
        var roles = await _roles.ListAsync(r => r.TenantId == tenantId, cancellationToken);
        var roleLookup = roles.ToDictionary(r => r.Id, r => r.Code);

        var users = await _users.ListAsync(u => u.TenantId == tenantId, cancellationToken);

        return users
            .Select(u => new UserResponse(
                u.Id,
                u.Email.Value,
                u.Name,
                u.IsActive,
                u.TwoFactorEnabled,
                u.RoleId,
                u.CreatedAtUtc,
                u.RoleId is not null && roleLookup.TryGetValue(u.RoleId.Value, out var code) ? code : null))
            .ToList();
    }
}