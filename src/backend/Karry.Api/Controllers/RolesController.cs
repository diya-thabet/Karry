using Karry.Application.Roles.Commands;
using Karry.Application.Roles.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Karry.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class RolesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RolesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Lists roles within the caller's tenant.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoleResponse>>> List(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new ListRolesQuery(), cancellationToken));

    /// <summary>Creates a custom role within the caller's tenant.</summary>
    [HttpPost]
    public async Task<ActionResult<CreateRoleResponse>> Create(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateRoleCommand(request), cancellationToken);
        return Ok(result);
    }
}