using Karry.Application.Tenants;
using Karry.Application.Tenants.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Karry.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TenantsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TenantsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a tenant, provisions the six system roles and the tenant's default unit
    /// preferences, and optionally an initial admin user. Restricted to platform admins.
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<CreateTenantResponse>> Create(
        [FromBody] CreateTenantRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateTenantCommand(request), cancellationToken);
        return CreatedAtAction(nameof(Create), new { id = result.TenantId }, result);
    }
}