using Karry.Application.Units.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Karry.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UnitsController : ControllerBase
{
    private readonly IMediator _mediator;

    public UnitsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Converts between volumetric (m³) and gravimetric (Tonnes / Short Tons) quantities
    /// using moisture-adjusted rock density. Mirrors M = V × ρ × κ_moisture.
    /// </summary>
    [HttpPost("convert")]
    public async Task<ActionResult<ConvertMeasureResponse>> Convert(
        [FromBody] ConvertMeasureRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ConvertMeasureCommand { Input = request },
            cancellationToken);

        return Ok(result);
    }
}