using Karry.Application.Users;
using Karry.Application.Users.Commands;
using Karry.Application.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Karry.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Lists users within the caller's tenant.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> List(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new ListUsersQuery(), cancellationToken));

    /// <summary>Creates a user within the caller's tenant, assigning an existing role.</summary>
    [HttpPost]
    public async Task<ActionResult<CreateUserResponse>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateUserCommand(request), cancellationToken);
        return Ok(result);
    }
}