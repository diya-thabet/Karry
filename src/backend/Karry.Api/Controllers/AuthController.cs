using Karry.Application.Auth;
using Karry.Application.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Karry.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Authenticates a user. Returns tokens, or a two-factor challenge when enabled.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new LoginCommand(request), cancellationToken);
        return Ok(result);
    }

    /// <summary>Completes login with a time-based one-time password when 2FA is enabled.</summary>
    [HttpPost("two-factor/login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> TwoFactorLogin(
        [FromBody] TwoFactorChallengeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new TwoFactorLoginCommand(request), cancellationToken);
        return Ok(result);
    }

    /// <summary>Rotates a refresh token, returning a new access + refresh pair.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthTokensResponse>> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RefreshCommand(request), cancellationToken);
        return Ok(result);
    }

    /// <summary>Revokes the supplied refresh token.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new LogoutCommand(request), cancellationToken);
        return NoContent();
    }

    /// <summary>Begins two-factor enrollment, returning a TOTP secret and OTP-auth URI.</summary>
    [HttpPost("two-factor/enable")]
    [Authorize]
    public async Task<ActionResult<EnableTwoFactorResponse>> EnableTwoFactor(
        [FromBody] EnableTwoFactorRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new EnableTwoFactorCommand(request), cancellationToken);
        return Ok(result);
    }

    /// <summary>Confirms enrollment by validating a code, persisting the TOTP secret.</summary>
    [HttpPost("two-factor/verify")]
    [Authorize]
    public async Task<IActionResult> VerifyTwoFactor(
        [FromBody] VerifyTwoFactorRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new VerifyTwoFactorCommand(request), cancellationToken);
        return NoContent();
    }

    /// <summary>Disables two-factor authentication for the current user.</summary>
    [HttpPost("two-factor/disable")]
    [Authorize]
    public async Task<IActionResult> DisableTwoFactor(CancellationToken cancellationToken)
    {
        await _mediator.Send(new DisableTwoFactorCommand(), cancellationToken);
        return NoContent();
    }
}