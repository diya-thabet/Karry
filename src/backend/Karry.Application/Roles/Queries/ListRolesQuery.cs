using Karry.Application.Common;
using Karry.Domain.Identity;
using Karry.Domain.Common;
using MediatR;

namespace Karry.Application.Roles.Queries;

public sealed record RoleResponse(Guid RoleId, string Code, string Name, string? Description, IReadOnlyList<string> Permissions);

public sealed record ListRolesQuery() : IRequest<IReadOnlyList<RoleResponse>>;

public sealed class ListRolesQueryHandler : IRequestHandler<ListRolesQuery, IReadOnlyList<RoleResponse>>
{
    private readonly IRepository<Role> _roles;
    private readonly ICurrentSession _session;

    public ListRolesQueryHandler(IRepository<Role> roles, ICurrentSession session)
    {
        _roles = roles;
        _session = session;
    }

    public async Task<IReadOnlyList<RoleResponse>> Handle(ListRolesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _session.TenantId
            ?? throw new ForbiddenException("Roles are tenant-scoped.");

        var roles = await _roles.ListAsync(r => r.TenantId == tenantId, cancellationToken);

        return roles
            .OrderBy(r => r.Code)
            .Select(r => new RoleResponse(
                r.Id,
                r.Code,
                r.Name,
                r.Description,
                r.Permissions.Select(p => $"{p.Resource}:{p.Action}").ToList()))
            .ToList();
    }
}