using Karry.Application.Common;
using Karry.Domain.Identity;
using Karry.Domain.Common;
using MediatR;

namespace Karry.Application.Roles.Commands;

public sealed record CreateRoleRequest(string Code, string Name, string? Description);

public sealed record CreateRoleResponse(Guid RoleId, string Code);

public sealed record CreateRoleCommand(CreateRoleRequest Input) : IRequest<CreateRoleResponse>;

/// <summary>
/// Creates a custom role within the current tenant's unit of work. Permissions come from the
/// canonical catalog so a custom role can only ever exercise known capabilities.
/// </summary>
public sealed class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, CreateRoleResponse>
{
    private readonly IRepository<Role> _roles;
    private readonly ICurrentSession _session;
    private readonly IClock _clock;

    public CreateRoleCommandHandler(IRepository<Role> roles, ICurrentSession session, IClock clock)
    {
        _roles = roles;
        _session = session;
        _clock = clock;
    }

    public async Task<CreateRoleResponse> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _session.TenantId
            ?? throw new ForbiddenException("Roles must be created within a tenant.");
        var actor = _session.UserId ?? Guid.Empty;

        var duplicate = await _roles.AnyAsync(r => r.TenantId == tenantId && r.Code == request.Input.Code.Trim().ToLowerInvariant(), cancellationToken);
        if (duplicate)
        {
            throw new ConflictException($"Role '{request.Input.Code}' already exists in this tenant.");
        }

        var role = Role.Create(tenantId, request.Input.Code, request.Input.Name, request.Input.Description, [], actor);
        await _roles.AddAsync(role, cancellationToken);

        return new CreateRoleResponse(role.Id, role.Code);
    }
}