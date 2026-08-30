using Karry.Application.Roles.Commands;
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